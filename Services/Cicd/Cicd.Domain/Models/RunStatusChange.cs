namespace Cicd.Domain.Models;

public sealed class RunStatusChange
{
    public long RunStatusHistoryId { get; set; }

    public long BuildRunId { get; set; }

    public string? FromStatus { get; set; }

    public string ToStatus { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}
