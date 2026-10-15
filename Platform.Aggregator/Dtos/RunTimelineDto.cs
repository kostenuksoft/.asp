namespace Platform.Aggregator.Dtos;

public sealed record RunTimelineDto(
    long BuildRunId,
    string PipelineName,
    int RunNumber,
    string Status,
    string BranchName,
    string CommitSha,
    DateTime QueuedAt,
    DateTime? FinishedAt,
    int? DurationSeconds,
    IReadOnlyList<RunArtifactDto> Artifacts
);

public sealed record RunArtifactDto(
    string UsageKind,
    long PackageId,
    string PackageKey,
    string SemVer
);
