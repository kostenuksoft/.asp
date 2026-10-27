using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Telemetry.Application.Common.Models;
using Telemetry.Application.Discussions.Commands.AddComment;
using Telemetry.Application.Discussions.Commands.ChangeDiscussionState;
using Telemetry.Application.Discussions.Commands.CreateDiscussion;
using Telemetry.Application.Discussions.Commands.DeleteDiscussion;
using Telemetry.Application.Discussions.Commands.UpdateDiscussion;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Application.Discussions.Queries.GetDiscussionsList;
using Telemetry.Domain.Enums;
using Telemetry.WebApi.Contracts;
using Telemetry.WebApi.Metrics;

namespace Telemetry.WebApi.Controllers;

[Authorize]
[Route("api/discussions")]
public sealed class DiscussionsController(
    ISender sender,
    TelemetryMetrics metrics
)
    : ApiController(sender)
{
    private const string CreateOperation = "create";

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<DiscussionSummaryDto>>> GetList(
        [FromQuery] string? packageKey,
        [FromQuery] DiscussionState? state,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default
    ) => Ok(await Sender.Send(
        new GetDiscussionsListQuery(packageKey, state, search, page, pageSize),
        cancellationToken
    ));

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DiscussionDetailsDto>> GetById(
        string id,
        CancellationToken cancellationToken
    ) => Ok(await Sender.Send(new GetDiscussionByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<string>> Create(
        CreateDiscussionCommand command,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            string id = await Sender.Send(command, cancellationToken);
            metrics.DiscussionCreated(command.Category);
            metrics.RecordDuration(CreateOperation, success: true, stopwatch.Elapsed.TotalMilliseconds);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (Exception) when (RecordFailure(CreateOperation, stopwatch))
        {
            throw;
        }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DiscussionDetailsDto>> Update(
        string id,
        UpdateDiscussionRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await Sender.Send(
            new UpdateDiscussionCommand(id, request.Title, request.Category, request.Labels, request.Version),
            cancellationToken
        ));
    }

    [HttpPost("{id}/comments")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DiscussionDetailsDto>> AddComment(
        string id,
        AddCommentRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await Sender.Send(
            new AddCommentCommand(id, request.Author, request.Body, request.Version),
            cancellationToken
        ));
    }

    [HttpPost("{id}/state")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DiscussionDetailsDto>> ChangeState(
        string id,
        ChangeDiscussionStateRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        return Ok(await Sender.Send(
            new ChangeDiscussionStateCommand(id, request.State, request.Version),
            cancellationToken
        ));
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        string id,
        [FromQuery, BindRequired] int version,
        CancellationToken cancellationToken
    )
    {
        await Sender.Send(new DeleteDiscussionCommand(id, version), cancellationToken);
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
