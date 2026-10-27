namespace Platform.Aggregator.Dtos;

public sealed record BuildRunSummaryDto(
    long BuildRunId,
    string PipelineName,
    int RunNumber,
    string Status,
    string BranchName,
    string CommitSha,
    DateTime QueuedAt,
    DateTime? FinishedAt,
    int? DurationSeconds
);
