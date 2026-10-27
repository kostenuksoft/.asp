using System.Data;
using System.Diagnostics;
using System.Text.Json;
using AutoMapper;
using Cicd.Bll.Dtos;
using Cicd.Bll.Services.Interfaces;
using Cicd.Dal.UnitOfWork;
using Cicd.Domain.Exceptions;
using Cicd.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Cicd.Bll.Services;

public sealed class BuildRunService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<BuildRunService> logger) : IBuildRunService
{
    private static readonly ActivitySource s_activitySource = new("Cicd.Api");

    public async Task<BuildRunDto> GetByIdAsync(
        long buildRunId,
        CancellationToken cancellationToken
    )
    {
        BuildRun run = await unitOfWork.BuildRuns.GetWithDetailAndStepsAsync(buildRunId, cancellationToken)
            ?? throw new NotFoundException($"Build run {buildRunId} was not found.");
        return mapper.Map<BuildRunDto>(run);
    }

    public async Task<IReadOnlyList<BuildRunDto>> GetByPipelineAsync(
        long pipelineId,
        CancellationToken cancellationToken
    )
    {
        _ = await unitOfWork.Pipelines.GetByIdAsync(pipelineId, cancellationToken)
            ?? throw new NotFoundException($"Pipeline {pipelineId} was not found.");

        IReadOnlyList<BuildRun> runs = await unitOfWork.BuildRuns.GetByPipelineAsync(pipelineId, cancellationToken);
        return mapper.Map<IReadOnlyList<BuildRunDto>>(runs);
    }

    public async Task<RunTimelineDto> GetTimelineAsync(
        long buildRunId,
        CancellationToken cancellationToken
    )
    {
        RunTimeline timeline = await unitOfWork.BuildRuns.GetTimelineAsync(buildRunId, cancellationToken)
            ?? throw new NotFoundException($"Build run {buildRunId} was not found.");
        return mapper.Map<RunTimelineDto>(timeline);
    }

    public async Task<BuildRunDto> QueueAsync(
        QueueBuildRunDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        using Activity? activity = s_activitySource.StartActivity("QueueBuildRun");
        activity?.SetTag("pipeline.id", input.PipelineId);

        Pipeline pipeline = await unitOfWork.Pipelines.GetByIdAsync(input.PipelineId, cancellationToken)
            ?? throw new NotFoundException($"Pipeline {input.PipelineId} was not found.");
        if (!pipeline.IsEnabled)
        {
            throw new BusinessConflictException($"Pipeline '{pipeline.Name}' is disabled and cannot queue new runs.");
        }

        if (input.TriggerPayload is not null && !IsJson(input.TriggerPayload))
        {
            throw new ValidationException("TriggerPayload must be a valid JSON document.");
        }

        BuildRunDetail detail = new()
        {
            CommitSha = input.CommitSha,
            CommitMessage = input.CommitMessage,
            BranchName = input.BranchName,
            TriggeredBy = input.TriggeredBy,
            TriggerKind = input.TriggerKind,
            TriggerPayload = input.TriggerPayload,
        };

        long buildRunId = await unitOfWork.BuildRuns.QueueAsync(new BuildRun { PipelineId = pipeline.PipelineId }, detail, cancellationToken);
        activity?.SetTag("build_run.id", buildRunId);
        logger.LogInformation(
            "Build run {BuildRunId} queued for pipeline {PipelineId} on branch {BranchName}, trigger {TriggerKind}",
            buildRunId,
            pipeline.PipelineId,
            input.BranchName,
            input.TriggerKind
        );

        return await GetByIdAsync(buildRunId, cancellationToken);
    }

    public async Task<BuildRunDto> StartAsync(
        long buildRunId,
        StartBuildRunDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        BuildRun run = await unitOfWork.BuildRuns.GetByIdAsync(buildRunId, cancellationToken)
            ?? throw new NotFoundException($"Build run {buildRunId} was not found.");
        if (run.Status != BuildRunStatus.Queued)
        {
            throw new BusinessConflictException($"Build run {buildRunId} is {run.Status}; only a queued run can be started.");
        }

        long? agentId = await unitOfWork.Agents.LeaseAsync(buildRunId, input.OperatingSystem, input.LeasedBy, cancellationToken);

        if (agentId is null)
        {
            throw new BusinessConflictException($"No idle {input.OperatingSystem} agent is available.");
        }

        return await GetByIdAsync(buildRunId, cancellationToken);
    }

    public async Task TransitionAsync(
        long buildRunId,
        TransitionBuildRunDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        byte[]? expectedRowVersion = DecodeRowVersion(input.ExpectedRowVersion);
        await unitOfWork.BuildRuns.TransitionStatusAsync(
            buildRunId,
            input.ToStatus,
            input.Reason,
            input.ChangedBy,
            expectedRowVersion,
            cancellationToken
        );
    }

    public async Task<RunTimelineDto> CompleteAsync(
        long buildRunId,
        CompleteBuildRunDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        using Activity? activity = s_activitySource.StartActivity("CompleteBuildRun");
        activity?.SetTag("build_run.id", buildRunId);
        activity?.SetTag("artifact.count", input.Artifacts.Count);

        bool committed = false;
        await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            BuildRun run = await unitOfWork.BuildRuns.GetByIdAsync(buildRunId, cancellationToken)
                ?? throw new NotFoundException($"Build run {buildRunId} was not found.");
            if (run.Status != BuildRunStatus.Running)
            {
                throw new BusinessConflictException($"Build run {buildRunId} is {run.Status}; only a running run can be completed.");
            }

            Pipeline pipeline = await unitOfWork.Pipelines.GetByIdAsync(run.PipelineId, cancellationToken)
                ?? throw new NotFoundException($"Pipeline {run.PipelineId} was not found.");
            if (pipeline.PackageId is null)
            {
                throw new BusinessConflictException($"Pipeline '{pipeline.Name}' is not linked to a registry package.");
            }

            Package package = await unitOfWork.Packages.GetByIdAsync(pipeline.PackageId.Value, cancellationToken)
                ?? throw new BusinessConflictException($"Package {pipeline.PackageId} has no local copy yet; it arrives with the Registry's events.");
            if (package.IsDeleted)
            {
                throw new BusinessConflictException($"Package '{package.PackageKey}' was deleted in the Registry.");
            }

            await unitOfWork.BuildRuns.TransitionStatusAsync(
                buildRunId,
                BuildRunStatus.Succeeded,
                $"Completed with {input.Artifacts.Count} artefact(s)",
                input.CompletedBy,
                run.RowVersion,
                cancellationToken
            );

            foreach (ProducedArtifactDto produced in input.Artifacts)
            {
                Artifact artifact = new()
                {
                    PackageId = package.PackageId,
                    PackageKey = package.PackageKey,
                    PublisherSlug = package.PublisherSlug,
                    PackageName = package.PackageName,
                    LicensePrice = package.LicensePrice,
                    RegistrySyncedAt = package.SyncedAt,
                    SemVer = produced.SemVer,
                    SizeBytes = produced.SizeBytes,
                    Sha256 = produced.Sha256,
                    StorageUri = produced.StorageUri,
                    CreatedBy = input.CompletedBy,
                };

                long artifactId = await unitOfWork.Artifacts.AddAsync(artifact, cancellationToken);
                await unitOfWork.Artifacts.LinkToRunAsync(
                    new ArtifactUsage
                    {
                        BuildRunId = buildRunId,
                        ArtifactId = artifactId,
                        UsageKind = ArtifactUsageKind.Produced,
                        CreatedBy = input.CompletedBy,
                    },
                    cancellationToken
                );
            }

            await unitOfWork.CommitAsync(cancellationToken);
            committed = true;
            logger.LogInformation("Build run {BuildRunId} completed with {ArtifactCount} artefact(s)", buildRunId, input.Artifacts.Count);
        }
        finally
        {
            if (!committed)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
                activity?.SetStatus(ActivityStatusCode.Error, "Completion rolled back");
                logger.LogWarning("Build run {BuildRunId} completion rolled back", buildRunId);
            }
        }

        return await GetTimelineAsync(buildRunId, cancellationToken);
    }

    public Task DeleteAsync(
        long buildRunId,
        CancellationToken cancellationToken
    ) => unitOfWork.BuildRuns.DeleteAsync(buildRunId, cancellationToken);

    private static byte[]? DecodeRowVersion(string? rowVersion)
    {
        if (string.IsNullOrEmpty(rowVersion))
        {
            return null;
        }

        try
        {
            byte[] decoded = Convert.FromBase64String(rowVersion);
            return decoded.Length == 8
                ? decoded
                : throw new ValidationException("ExpectedRowVersion must encode exactly 8 bytes.");
        }
        catch (FormatException exception)
        {
            throw new ValidationException("ExpectedRowVersion must be a Base64 string.", exception);
        }
    }

    private static bool IsJson(string text)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
