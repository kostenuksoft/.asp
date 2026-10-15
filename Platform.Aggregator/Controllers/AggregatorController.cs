using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Dtos;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/aggregator")]
[Produces("application/json")]
public sealed class AggregatorController(BuildRunCardService cards) : ControllerBase
{
    [HttpGet("build-runs/{id:long}/full")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BuildRunCardDto>> GetBuildRunCard(
        long id,
        CancellationToken cancellationToken
    )
    {
        BuildRunCardDto? card = await cards.GetAsync(id, cancellationToken);
        return card is null ? NotFound() : Ok(card);
    }
}
