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
    IReadOnlyList<RunArtifactDto> Artifacts
);
