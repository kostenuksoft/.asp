using Telemetry.Application.Common.Messaging;
using Telemetry.Domain.Enums;

namespace Telemetry.Application.Discussions.Commands.CreateDiscussion;

public sealed record CreateDiscussionCommand(
    long PackageId,
    string PackageKey,
    string PackageName,
    string PublisherSlug,
    string Title,
    DiscussionCategory Category,
    string OpenedBy
) : ICommand<string>;
