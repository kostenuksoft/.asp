using AutoMapper;
using Cicd.Bll.Dtos;
using Cicd.Bll.Services.Interfaces;
using Cicd.Dal.UnitOfWork;
using Cicd.Domain.Exceptions;
using Cicd.Domain.Models;

namespace Cicd.Bll.Services;

public sealed class AgentService(IUnitOfWork unitOfWork, IMapper mapper) : IAgentService
{
    private static readonly HashSet<string> s_statuses =
        [AgentStatus.Idle, AgentStatus.Leased, AgentStatus.Offline, AgentStatus.Draining];

    public async Task<AgentDto> GetByIdAsync(long agentId, CancellationToken cancellationToken)
    {
        Agent agent = await unitOfWork.Agents.GetByIdAsync(agentId, cancellationToken)
            ?? throw new NotFoundException($"Agent {agentId} was not found.");
        return mapper.Map<AgentDto>(agent);
    }

    public async Task<IReadOnlyList<AgentDto>> GetByStatusAsync(string status, CancellationToken cancellationToken)
    {
        if (!s_statuses.Contains(status))
        {
            throw new ValidationException($"Unknown agent status '{status}'.");
        }

        IReadOnlyList<Agent> agents = await unitOfWork.Agents.GetByStatusAsync(status, cancellationToken);
        return mapper.Map<IReadOnlyList<AgentDto>>(agents);
    }
}
