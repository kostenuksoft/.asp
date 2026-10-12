namespace Cicd.Domain.Models;

public sealed class ArtifactUsage
{
    public long BuildRunId { get; set; }

    public long ArtifactId { get; set; }

    public string UsageKind { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}
