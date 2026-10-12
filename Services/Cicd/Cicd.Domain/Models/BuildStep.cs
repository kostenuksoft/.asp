namespace Cicd.Domain.Models;

public sealed class BuildStep
{
    public long BuildStepId { get; set; }

    public long BuildRunId { get; set; }

    public int StepOrder { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int? ExitCode { get; set; }
}
