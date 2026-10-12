namespace Cicd.Domain.Models;

public sealed class BuildRunDetail
{
    public long BuildRunId { get; set; }

    public string CommitSha { get; set; } = string.Empty;

    public string CommitMessage { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public string TriggeredBy { get; set; } = string.Empty;

    public string TriggerKind { get; set; } = string.Empty;

    public string? TriggerPayload { get; set; }
}
