namespace Cicd.Bll.Dtos;

public sealed record BuildRunDto(
    long BuildRunId,
    long PipelineId,
    long? AgentId,
    int RunNumber,
    string Status,
    DateTime QueuedAt,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    int? DurationSeconds,
    string RowVersion,
    BuildRunDetailDto? Detail,
    IReadOnlyList<BuildStepDto> Steps
);

public sealed record BuildRunDetailDto(
    string CommitSha,
    string CommitMessage,
    string BranchName,
    string TriggeredBy,
    string TriggerKind,
    string? TriggerPayload
);

public sealed record BuildStepDto(
    long BuildStepId,
    int StepOrder,
    string Name,
    string Status,
    DateTime? StartedAt,
    DateTime? FinishedAt,
    int? ExitCode
);
