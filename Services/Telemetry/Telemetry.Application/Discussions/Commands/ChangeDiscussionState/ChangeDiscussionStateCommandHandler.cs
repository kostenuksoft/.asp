using System.Diagnostics;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Application.Discussions.Commands.ChangeDiscussionState;

public sealed class ChangeDiscussionStateCommandHandler(
    IDiscussionRepository discussions,
    IMapper mapper,
    ILogger<ChangeDiscussionStateCommandHandler> logger
)
    : IRequestHandler<ChangeDiscussionStateCommand, DiscussionDetailsDto>
{
    private static readonly ActivitySource s_activitySource = new("Telemetry.WebApi");

    public async Task<DiscussionDetailsDto> Handle(
        ChangeDiscussionStateCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        using Activity? activity = s_activitySource.StartActivity("ChangeDiscussionState");
        activity?.SetTag("discussion.id", request.Id);

        Discussion discussion = await discussions.GetAtVersionAsync(request.Id, request.Version, cancellationToken);
        DiscussionState previousState = discussion.State;
        discussion.ChangeState(request.State);
        await discussions.UpdateAsync(discussion, cancellationToken);

        activity?.SetTag("discussion.state", discussion.State.ToString());
        logger.LogInformation(
            "Discussion {DiscussionId} moved from {PreviousState} to {State}",
            discussion.Id,
            previousState,
            discussion.State
        );

        return mapper.Map<DiscussionDetailsDto>(discussion);
    }
}
