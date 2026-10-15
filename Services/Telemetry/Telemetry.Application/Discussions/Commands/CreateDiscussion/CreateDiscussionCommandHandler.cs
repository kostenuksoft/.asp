using MediatR;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Application.Discussions.Commands.CreateDiscussion;

public sealed class CreateDiscussionCommandHandler(IDiscussionRepository discussions)
    : IRequestHandler<CreateDiscussionCommand, string>
{
    private const string CreatedBy = "telemetry-api";

    public async Task<string> Handle(
        CreateDiscussionCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        Discussion discussion = new(
            request.PackageId,
            request.PackageKey,
            request.PackageName,
            request.PublisherSlug,
            request.Title,
            request.Category,
            request.OpenedBy,
            CreatedBy
        );

        await discussions.AddAsync(discussion, cancellationToken);
        return discussion.Id.ToString();
    }
}
