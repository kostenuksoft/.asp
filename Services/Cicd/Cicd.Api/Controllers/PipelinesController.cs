using Cicd.Api.Authorization;
using Cicd.Bll.Dtos;
using Cicd.Bll.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cicd.Api.Controllers;

[ApiController]
[Route("api/pipelines")]
[Produces("application/json")]
public sealed class PipelinesController(
    IPipelineService pipelines,
    IBuildRunService buildRuns
)
    : ControllerBase
{
    [HttpGet]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PipelineDto>>> GetAll(
        CancellationToken cancellationToken
    ) => Ok(await pipelines.GetAllAsync(cancellationToken));

    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PipelineDto>> GetById(
        long id,
        CancellationToken cancellationToken
    ) => Ok(await pipelines.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:long}/build-runs")]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<BuildRunDto>>> GetBuildRuns(
        long id,
        CancellationToken cancellationToken
    ) => Ok(await buildRuns.GetByPipelineAsync(id, cancellationToken));

    [HttpPost]
    [RequirePermission(Permissions.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PipelineDto>> Create(
        CreatePipelineDto input,
        CancellationToken cancellationToken
    )
    {
        PipelineDto created = await pipelines.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.PipelineId }, created);
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken
    )
    {
        await pipelines.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
