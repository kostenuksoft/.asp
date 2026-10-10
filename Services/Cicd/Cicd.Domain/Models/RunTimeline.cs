namespace Cicd.Domain.Models;

public sealed class RunTimeline
{
    public long BuildRunId { get; set; }

    public long PipelineId { get; set; }

    public string PipelineName { get; set; } = string.Empty;

    public string RepositoryUrl { get; set; } = string.Empty;

    public int RunNumber { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime QueuedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int? DurationSeconds { get; set; }

    public long? AgentId { get; set; }

    public string? AgentHostname { get; set; }

    public string CommitSha { get; set; } = string.Empty;

    public string CommitMessage { get; set; } = string.Empty;

    public string BranchName { get; set; } = string.Empty;

    public string TriggeredBy { get; set; } = string.Empty;

    public string TriggerKind { get; set; } = string.Empty;

    public byte[] RowVersion { get; set; } = [];

    public IReadOnlyList<BuildStep> Steps { get; set; } = [];

    public IReadOnlyList<RunStatusChange> History { get; set; } = [];

    public IReadOnlyList<RunArtifact> Artifacts { get; set; } = [];
}
