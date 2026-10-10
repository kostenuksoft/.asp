namespace Cicd.Bll.Dtos;

public sealed record RunTimelineDto(
    long BuildRunId,
    long PipelineId,
    string PipelineName,
    string RepositoryUrl,
    int RunNumber,
    string Status,
    DateTime QueuedAt,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    int? DurationSeconds,
    long? AgentId,
    string? AgentHostname,
    string CommitSha,
    string CommitMessage,
    string BranchName,
    string TriggeredBy,
    string TriggerKind,
    string RowVersion,
    IReadOnlyList<BuildStepDto> Steps,
    IReadOnlyList<RunStatusChangeDto> History,
    IReadOnlyList<RunArtifactDto> Artifacts);

public sealed record RunStatusChangeDto(
    string? FromStatus,
    string ToStatus,
    string? Reason,
    DateTime CreatedAt,
    string CreatedBy);

public sealed record RunArtifactDto(
    string UsageKind,
    long ArtifactId,
    long PackageId,
    string PackageKey,
    string PackageName,
    string SemVer,
    long SizeBytes,
    string StorageUri);
