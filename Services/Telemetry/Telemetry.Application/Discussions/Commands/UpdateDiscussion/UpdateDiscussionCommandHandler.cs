using AutoMapper;
using MediatR;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Application.Discussions.Commands.UpdateDiscussion;

public sealed class UpdateDiscussionCommandHandler(
    IDiscussionRepository discussions,
    IMapper mapper
)
    : IRequestHandler<UpdateDiscussionCommand, DiscussionDetailsDto>
{
    public async Task<DiscussionDetailsDto> Handle(
        UpdateDiscussionCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        Discussion discussion = await discussions.GetAtVersionAsync(request.Id, request.Version, cancellationToken);
        discussion.Edit(request.Title, request.Category, request.Labels);
        await discussions.UpdateAsync(discussion, cancellationToken);
        return mapper.Map<DiscussionDetailsDto>(discussion);
    }
}
