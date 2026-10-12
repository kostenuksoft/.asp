using Cicd.Bll.Dtos;
using Cicd.Bll.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cicd.Api.Controllers;

[ApiController]
[Route("api/agents")]
[Produces("application/json")]
public sealed class AgentsController(IAgentService agents) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AgentDto>>> GetByStatus([FromQuery] string status, CancellationToken cancellationToken)
        => Ok(await agents.GetByStatusAsync(status, cancellationToken));

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgentDto>> GetById(long id, CancellationToken cancellationToken)
        => Ok(await agents.GetByIdAsync(id, cancellationToken));
}
