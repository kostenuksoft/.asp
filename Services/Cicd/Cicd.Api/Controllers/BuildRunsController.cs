using System.Diagnostics;
using Cicd.Api.Authorization;
using Cicd.Api.Metrics;
using Cicd.Bll.Dtos;
using Cicd.Bll.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cicd.Api.Controllers;

[ApiController]
[Route("api/build-runs")]
[Produces("application/json")]
public sealed class BuildRunsController(IBuildRunService buildRuns, CicdMetrics metrics) : ControllerBase
{
    private const string QueueOperation = "queue";
    private const string CompleteOperation = "complete";

    [HttpGet("{id:long}")]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BuildRunDto>> GetById(
        long id,
        CancellationToken cancellationToken
    ) => Ok(await buildRuns.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:long}/timeline")]
    [RequirePermission(Permissions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunTimelineDto>> GetTimeline(
        long id,
        CancellationToken cancellationToken
    ) => Ok(await buildRuns.GetTimelineAsync(id, cancellationToken));

    [HttpPost]
    [RequirePermission(Permissions.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BuildRunDto>> Queue(
        QueueBuildRunDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            BuildRunDto queued = await buildRuns.QueueAsync(input, cancellationToken);
            metrics.RunQueued(input.TriggerKind);
            metrics.RecordDuration(QueueOperation, success: true, stopwatch.Elapsed.TotalMilliseconds);
            return CreatedAtAction(nameof(GetById), new { id = queued.BuildRunId }, queued);
        }
        catch (Exception) when (RecordFailure(QueueOperation, stopwatch))
        {
            throw;
        }
    }

    [HttpPost("{id:long}/start")]
    [RequirePermission(Permissions.Approve)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BuildRunDto>> Start(
        long id,
        StartBuildRunDto input,
        CancellationToken cancellationToken
    ) => Ok(await buildRuns.StartAsync(id, input, cancellationToken));

    [HttpPost("{id:long}/transition")]
    [RequirePermission(Permissions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Transition(
        long id,
        TransitionBuildRunDto input,
        CancellationToken cancellationToken
    )
    {
        await buildRuns.TransitionAsync(id, input, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:long}/complete")]
    [RequirePermission(Permissions.Update)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RunTimelineDto>> Complete(
        long id,
        CompleteBuildRunDto input,
        CancellationToken cancellationToken
    )
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            RunTimelineDto timeline = await buildRuns.CompleteAsync(id, input, cancellationToken);
            metrics.RunCompleted(timeline.Status);
            metrics.RecordDuration(CompleteOperation, success: true, stopwatch.Elapsed.TotalMilliseconds);
            return Ok(timeline);
        }
        catch (Exception) when (RecordFailure(CompleteOperation, stopwatch))
        {
            throw;
        }
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(Permissions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken
    )
    {
        await buildRuns.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private bool RecordFailure(
        string operation,
        Stopwatch stopwatch
    )
    {
        metrics.RecordDuration(operation, success: false, stopwatch.Elapsed.TotalMilliseconds);
        return false;
    }
}
