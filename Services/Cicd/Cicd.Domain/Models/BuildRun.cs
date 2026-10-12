namespace Cicd.Domain.Models;

public sealed class BuildRun : AuditableEntity
{
    public long BuildRunId { get; set; }

    public long PipelineId { get; set; }

    public long? AgentId { get; set; }

    public int RunNumber { get; set; }

    public string Status { get; set; } = BuildRunStatus.Queued;

    public DateTime QueuedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int? DurationSeconds { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public BuildRunDetail? Detail { get; set; }

    public IList<BuildStep> Steps { get; } = [];
}
