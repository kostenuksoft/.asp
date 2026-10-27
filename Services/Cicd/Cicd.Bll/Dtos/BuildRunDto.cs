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
