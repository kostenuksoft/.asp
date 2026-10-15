using System.Diagnostics;
using Platform.Aggregator.Clients;
using Platform.Aggregator.Dtos;
using Polly;

namespace Platform.Aggregator.Services;

public sealed class BuildRunCardService(
    CicdClient cicd,
    RegistryClient registry,
    TelemetryClient telemetry,
    ILogger<BuildRunCardService> logger
)
{
    private const int ItemsPerPackage = 5;
    private const string CicdService = "cicd-api";
    private const string RegistryService = "registry-api";
    private const string TelemetryService = "telemetry-api";

    public async Task<BuildRunCardDto?> GetAsync(
        long buildRunId,
        CancellationToken cancellationToken
    )
    {
        RunTimelineDto? run = await CallAsync(
            CicdService,
            $"build run {buildRunId}",
            () => cicd.GetTimelineAsync(buildRunId, cancellationToken)
        );
        if (run is null)
        {
            return null;
        }

        List<string> warnings = [];
        List<IGrouping<long, RunArtifactDto>> artifactsByPackage = [.. run.Artifacts.GroupBy(artifact => artifact.PackageId)];

        Task<PackageDto?>[] packageTasks = [.. artifactsByPackage.Select(group => SafeAsync(
            RegistryService,
            $"package {group.Key}",
            () => registry.GetPackageAsync(group.Key, cancellationToken),
            warnings,
            cancellationToken
        ))];
        Task<PagedResultDto<DiscussionDto>?>[] discussionTasks = [.. artifactsByPackage.Select(group => SafeAsync(
            TelemetryService,
            $"discussions of {group.First().PackageKey}",
            () => telemetry.GetDiscussionsAsync(group.First().PackageKey, ItemsPerPackage, cancellationToken),
            warnings,
            cancellationToken
        ))];
        Task<PagedResultDto<CrashReportDto>?>[] crashReportTasks = [.. artifactsByPackage.Select(group => SafeAsync(
            TelemetryService,
            $"crash reports of {group.First().PackageKey}",
            () => telemetry.GetCrashReportsAsync(group.First().PackageKey, ItemsPerPackage, cancellationToken),
            warnings,
            cancellationToken
        ))];

        await Task.WhenAll([.. packageTasks, .. discussionTasks, .. crashReportTasks]);

        List<PackageCardDto> packages = new(artifactsByPackage.Count);
        for (int index = 0; index < artifactsByPackage.Count; index++)
        {
            packages.Add(ToCard(
                artifactsByPackage[index],
                await packageTasks[index],
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
            run.QueuedAt,
            run.FinishedAt,
            run.DurationSeconds
        );

        return new BuildRunCardDto(summary, packages, warnings);
    }

    private static PackageCardDto ToCard(
        IGrouping<long, RunArtifactDto> artifacts,
        PackageDto? package,
        PagedResultDto<DiscussionDto>? discussions,
        PagedResultDto<CrashReportDto>? crashReports
    )
    {
        string packageKey = artifacts.First().PackageKey;
        PackageDto? owned = package?.PackageId == artifacts.Key ? package : null;

        return new PackageCardDto(
            artifacts.Key,
            packageKey,
            [.. artifacts.Select(artifact => artifact.SemVer).Distinct()],
            owned?.Description,
            owned?.LicensePrice,
            owned?.DownloadCount,
            owned?.IsListed,
            discussions?.TotalCount,
            discussions?.Items.Where(discussion => discussion.PackageKey == packageKey).ToList(),
            crashReports?.TotalCount,
            crashReports?.Items.Where(report => report.PackageKey == packageKey).ToList()
        );
    }

    private async Task<T?> SafeAsync<T>(
        string service,
        string target,
        Func<Task<T?>> call,
        List<string> warnings,
        CancellationToken cancellationToken
    )
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

            return default;
        }
    }

    private async Task<T?> CallAsync<T>(
        string service,
        string target,
        Func<Task<T?>> call
    )
    {
        logger.LogInformation("Calling {Service} for {Target}", service, target);
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            T? result = await call();
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
            "{Service} failed for {Target} after {ElapsedMilliseconds} ms",
            service,
            target,
            stopwatch.ElapsedMilliseconds
        );
        return false;
    }

    internal static bool IsUnavailable(
        Exception exception,
        CancellationToken cancellationToken
    ) => exception is HttpRequestException or ExecutionRejectedException
        || (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
}
