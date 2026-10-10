namespace Cicd.Bll.Dtos;

public sealed record AgentDto(
    long AgentId,
    string Hostname,
    string OperatingSystem,
    string Status,
    DateTime? LastHeartbeatAt);
