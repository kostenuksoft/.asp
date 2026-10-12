using Cicd.Bll.Dtos;

namespace Cicd.Bll.Services.Interfaces;

public interface IAgentService
{
    Task<AgentDto> GetByIdAsync(
        long agentId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<AgentDto>> GetByStatusAsync(
        string status,
        CancellationToken cancellationToken
    );
}
