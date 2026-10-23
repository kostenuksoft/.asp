using System.Diagnostics;
using System.Text.Json;
using Cicd.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Caching.Distributed;
using Platform.Aggregator.Dtos;
using Polly;
using Registry.Grpc;
using StackExchange.Redis;
using Telemetry.Grpc;

namespace Platform.Aggregator.Services;

public sealed class BuildRunCardService(
    CicdService.CicdServiceClient cicd,
    RegistryService.RegistryServiceClient registry,
    TelemetryService.TelemetryServiceClient telemetry,
    IDistributedCache cache,
    ILogger<BuildRunCardService> logger
)
{
    private const int ItemsPerPackage = 5;
    private const decimal NanosPerUnit = 1_000_000_000m;
    private const string CicdService = "cicd-api";
    private const string RegistryService = "registry-api";
    private const string TelemetryService = "telemetry-api";

    private static readonly TimeSpan s_cardTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan s_callDeadline = TimeSpan.FromSeconds(3);
    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    public async Task<BuildRunCardDto?> GetAsync(
        long buildRunId,
        CancellationToken cancellationToken
    )
    {
        string key = $"aggregator:build-run:{buildRunId}:full";

        BuildRunCardDto? cached = await ReadCardAsync(key, cancellationToken);
        if (cached is not null)
        {
            logger.LogInformation("Cache hit {CacheKey}, no gRPC calls", key);
            return cached;
        }

        logger.LogInformation("Cache miss {CacheKey}, calling the services", key);
        BuildRunCardDto? card = await BuildAsync(buildRunId, cancellationToken);
        if (card is null)
        {
            return null;
        }

        if (card.Warnings.Count > 0)
        {
            logger.LogInformation("Card {CacheKey} not cached, partial answer with {WarningCount} warnings", key, card.Warnings.Count);
            return card;
        }

        await WriteCardAsync(key, card, cancellationToken);
        return card;
    }

    private async Task<BuildRunCardDto?> BuildAsync(
        long buildRunId,
        CancellationToken cancellationToken
    )
    {
        RunTimelineReply run;
        try
        {
            run = await CallAsync(
                CicdService,
                $"build run {buildRunId}",
                () => cicd.GetRunTimelineAsync(
                    new GetRunTimelineRequest { BuildRunId = buildRunId },
                    deadline: Deadline(),
                    cancellationToken: cancellationToken
                ).ResponseAsync
            );
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return null;
        }

        List<string> warnings = [];
        List<IGrouping<long, RunArtifact>> artifactsByPackage = [.. run.Artifacts.GroupBy(artifact => artifact.PackageId)];
        long[] packageIds = [.. artifactsByPackage.Select(group => group.Key)];

        Task<PackagesReply?> packagesTask = packageIds.Length == 0
            ? Task.FromResult<PackagesReply?>(null)
            : SafeAsync(
                RegistryService,
                $"packages {string.Join(',', packageIds)}",
                () => registry.GetPackagesByIdsAsync(
                    new GetPackagesByIdsRequest { PackageIds = { packageIds } },
                    deadline: Deadline(),
                    cancellationToken: cancellationToken
                ).ResponseAsync,
                warnings,
                cancellationToken
            );
        Task<DiscussionsReply?>[] discussionTasks = [.. artifactsByPackage.Select(group => SafeAsync(
            TelemetryService,
            $"discussions of {group.First().PackageKey}",
            () => telemetry.GetDiscussionsAsync(
                new PackageItemsRequest { PackageKey = group.First().PackageKey, PageSize = ItemsPerPackage },
                deadline: Deadline(),
                cancellationToken: cancellationToken
            ).ResponseAsync,
            warnings,
            cancellationToken
        ))];
        Task<CrashReportsReply?>[] crashReportTasks = [.. artifactsByPackage.Select(group => SafeAsync(
            TelemetryService,
            $"crash reports of {group.First().PackageKey}",
            () => telemetry.GetCrashReportsAsync(
                new PackageItemsRequest { PackageKey = group.First().PackageKey, PageSize = ItemsPerPackage },
                deadline: Deadline(),
                cancellationToken: cancellationToken
            ).ResponseAsync,
            warnings,
            cancellationToken
        ))];

        await Task.WhenAll([packagesTask, .. discussionTasks, .. crashReportTasks]);

        PackagesReply? packages = await packagesTask;
        List<PackageCardDto> cards = new(artifactsByPackage.Count);
        for (int index = 0; index < artifactsByPackage.Count; index++)
        {
            cards.Add(ToCard(
                artifactsByPackage[index],
                packages?.Packages.FirstOrDefault(package => package.PackageId == artifactsByPackage[index].Key),
                await discussionTasks[index],
                await crashReportTasks[index]
            ));
        }

        BuildRunSummaryDto summary = new(
            run.BuildRunId,
            run.PipelineName,
            run.RunNumber,
            run.Status,
            run.BranchName,
            run.CommitSha,
            run.QueuedAt.ToDateTime(),
            run.FinishedAt?.ToDateTime(),
            run.HasDurationSeconds ? run.DurationSeconds : null
        );

        return new BuildRunCardDto(summary, cards, warnings);
    }

    private static PackageCardDto ToCard(
        IGrouping<long, RunArtifact> artifacts,
        PackageReply? owned,
        DiscussionsReply? discussions,
        CrashReportsReply? crashReports
    )
    {
        string packageKey = artifacts.First().PackageKey;

        return new PackageCardDto(
            artifacts.Key,
            packageKey,
            [.. artifacts.Select(artifact => artifact.SemVer).Distinct()],
            owned is { HasDescription: true } ? owned.Description : null,
            owned is null ? null : owned.LicensePrice.Units + (owned.LicensePrice.Nanos / NanosPerUnit),
            owned?.DownloadCount,
            owned?.IsListed,
            discussions?.TotalCount,
            discussions?.Items
                .Where(discussion => discussion.PackageKey == packageKey)
                .Select(discussion => new DiscussionDto(
                    discussion.Id,
                    discussion.PackageKey,
                    discussion.Title,
                    discussion.Category,
                    discussion.State,
                    discussion.CommentCount,
                    discussion.OpenedAt.ToDateTime()
                ))
                .ToList(),
            crashReports?.TotalCount,
            crashReports?.Items
                .Where(report => report.PackageKey == packageKey)
                .Select(report => new CrashReportDto(
                    report.Id,
                    report.PackageKey,
                    report.Kind,
                    report.HasSeverity ? report.Severity : null,
                    report.Fingerprint,
                    report.OccurredAt.ToDateTime()
                ))
                .ToList()
        );
    }

    private async Task<BuildRunCardDto?> ReadCardAsync(
        string key,
        CancellationToken cancellationToken
    )
    {
        try
        {
            byte[]? stored = await cache.GetAsync(key, cancellationToken);
            return stored is null ? null : JsonSerializer.Deserialize<BuildRunCardDto>(stored, s_json);
        }
        catch (Exception exception) when (exception is RedisException or RedisTimeoutException)
        {
            logger.LogWarning(exception, "Redis unavailable, {CacheKey} read as a miss", key);
            return null;
        }
    }

    private async Task WriteCardAsync(
        string key,
        BuildRunCardDto card,
        CancellationToken cancellationToken
    )
    {
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = s_cardTtl
        };
        try
        {
            await cache.SetAsync(key, JsonSerializer.SerializeToUtf8Bytes(card, s_json), options, cancellationToken);
            logger.LogInformation("Card {CacheKey} cached for {CacheTtl}", key, s_cardTtl);
        }
        catch (Exception exception) when (exception is RedisException or RedisTimeoutException)
        {
            logger.LogWarning(exception, "Redis unavailable, {CacheKey} not cached", key);
        }
    }

    private async Task<T?> SafeAsync<T>(
        string service,
        string target,
        Func<Task<T>> call,
        List<string> warnings,
        CancellationToken cancellationToken
    )
        where T : class
    {
        try
        {
            return await CallAsync(service, target, call);
        }
        catch (Exception exception) when (IsUnavailable(exception, cancellationToken))
        {
            string warning = $"{service} is unavailable";
            lock (warnings)
            {
                if (!warnings.Contains(warning))
                {
                    warnings.Add(warning);
                }
            }

            return null;
        }
    }

    private async Task<T> CallAsync<T>(
        string service,
        string target,
        Func<Task<T>> call
    )
    {
        logger.LogInformation("Calling {Service} over gRPC for {Target}", service, target);
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            T result = await call();
            logger.LogInformation(
                "{Service} responded for {Target} in {ElapsedMilliseconds} ms",
                service,
                target,
                stopwatch.ElapsedMilliseconds
            );
            return result;
        }
        catch (Exception exception) when (LogFailure(service, target, stopwatch, exception))
        {
            throw;
        }
    }

    private bool LogFailure(
        string service,
        string target,
        Stopwatch stopwatch,
        Exception exception
    )
    {
        logger.LogWarning(
            exception,
            "{Service} failed for {Target} after {ElapsedMilliseconds} ms with {GrpcStatus}",
            service,
            target,
            stopwatch.ElapsedMilliseconds,
            (exception as RpcException)?.StatusCode
        );
        return false;
    }

    private static DateTime Deadline() => DateTime.UtcNow.Add(s_callDeadline);

    internal static bool IsUnavailable(
        Exception exception,
        CancellationToken cancellationToken
    ) => exception switch
    {
        RpcException { StatusCode: StatusCode.Unavailable or StatusCode.DeadlineExceeded } => true,
        RpcException { InnerException: HttpRequestException or ExecutionRejectedException } => true,
        RpcException { StatusCode: StatusCode.Cancelled } => !cancellationToken.IsCancellationRequested,
        HttpRequestException or ExecutionRejectedException => true,
        TaskCanceledException => !cancellationToken.IsCancellationRequested,
        _ => false
    };
}
