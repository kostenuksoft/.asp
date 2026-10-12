using Cicd.Domain.Models;

namespace Cicd.Dal.Repositories.Interfaces;

public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(long agentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Agent>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);

    Task<long?> LeaseAsync(long buildRunId, string operatingSystem, string leasedBy, CancellationToken cancellationToken = default);
}
