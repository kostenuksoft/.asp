using Cicd.Bll.Dtos;
using Cicd.Bll.Services.Interfaces;
using Cicd.Domain.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cicd.Api.HealthChecks;

public sealed class AgentPoolHealthCheck(IAgentService agents) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyList<AgentDto> idle = await agents.GetByStatusAsync(AgentStatus.Idle, cancellationToken);
        Dictionary<string, object> data = new()
        {
            ["idleAgents"] = idle.Count,
        };

        return idle.Count > 0
            ? HealthCheckResult.Healthy($"{idle.Count} idle agents", data)
            : new HealthCheckResult(context.Registration.FailureStatus, "No idle agent, queued runs wait", data: data);
    }
}
