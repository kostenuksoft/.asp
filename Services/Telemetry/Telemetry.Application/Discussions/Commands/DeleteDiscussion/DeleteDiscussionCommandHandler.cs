using MediatR;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Application.Discussions.Commands.DeleteDiscussion;

public sealed class DeleteDiscussionCommandHandler(IDiscussionRepository discussions)
    : IRequestHandler<DeleteDiscussionCommand>
{
    public async Task Handle(
        DeleteDiscussionCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        Discussion discussion = await discussions.GetAtVersionAsync(request.Id, request.Version, cancellationToken);
        await discussions.DeleteAsync(discussion, cancellationToken);
    }
}
