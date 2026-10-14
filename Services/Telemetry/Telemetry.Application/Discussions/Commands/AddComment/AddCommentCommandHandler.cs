using AutoMapper;
using MediatR;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Application.Discussions.Commands.AddComment;

public sealed class AddCommentCommandHandler(
    IDiscussionRepository discussions,
    IMapper mapper
)
    : IRequestHandler<AddCommentCommand, DiscussionDetailsDto>
{
    public async Task<DiscussionDetailsDto> Handle(
        AddCommentCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        Discussion discussion = await discussions.GetAtVersionAsync(request.Id, request.Version, cancellationToken);
        discussion.AddComment(request.Author, request.Body);
        await discussions.UpdateAsync(discussion, cancellationToken);
        return mapper.Map<DiscussionDetailsDto>(discussion);
    }
}
