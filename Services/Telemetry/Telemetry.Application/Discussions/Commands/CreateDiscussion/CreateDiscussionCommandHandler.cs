using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Application.Discussions.Commands.CreateDiscussion;

public sealed class CreateDiscussionCommandHandler(
    IDiscussionRepository discussions,
    ILogger<CreateDiscussionCommandHandler> logger
)
    : IRequestHandler<CreateDiscussionCommand, string>
{
    private const string CreatedBy = "telemetry-api";

    private static readonly ActivitySource s_activitySource = new("Telemetry.WebApi");

    public async Task<string> Handle(
        CreateDiscussionCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        using Activity? activity = s_activitySource.StartActivity("CreateDiscussion");
        activity?.SetTag("package.id", request.PackageId);

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

        activity?.SetTag("discussion.id", discussion.Id.ToString());
        logger.LogInformation(
            "Discussion {DiscussionId} opened for package {PackageKey}, category {Category}",
            discussion.Id,
            discussion.PackageKey,
            discussion.Category
        );

        return discussion.Id.ToString();
    }
}
