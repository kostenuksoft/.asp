namespace Cicd.Bll.Dtos;

public sealed record BuildRunDetailDto(
    string CommitSha,
    string CommitMessage,
    string BranchName,
    string TriggeredBy,
    string TriggerKind,
    string? TriggerPayload
);
