namespace Cicd.Domain.Models;

public sealed class Agent : AuditableEntity
{
    public long AgentId { get; set; }

    public string Hostname { get; set; } = string.Empty;

    public string OperatingSystem { get; set; } = string.Empty;

    public string Status { get; set; } = AgentStatus.Idle;

    public DateTime? LastHeartbeatAt { get; set; }
}
