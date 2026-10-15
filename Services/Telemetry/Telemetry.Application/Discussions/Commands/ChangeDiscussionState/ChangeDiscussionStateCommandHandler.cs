using AutoMapper;
using MediatR;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Application.Discussions.Commands.ChangeDiscussionState;

public sealed class ChangeDiscussionStateCommandHandler(
    IDiscussionRepository discussions,
    IMapper mapper
)
    : IRequestHandler<ChangeDiscussionStateCommand, DiscussionDetailsDto>
{
    public async Task<DiscussionDetailsDto> Handle(
        ChangeDiscussionStateCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        Discussion discussion = await discussions.GetAtVersionAsync(request.Id, request.Version, cancellationToken);
        discussion.ChangeState(request.State);
        await discussions.UpdateAsync(discussion, cancellationToken);
        return mapper.Map<DiscussionDetailsDto>(discussion);
    }
}
