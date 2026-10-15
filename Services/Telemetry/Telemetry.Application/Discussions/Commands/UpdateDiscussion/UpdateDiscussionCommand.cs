using Telemetry.Application.Common.Messaging;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Domain.Enums;

namespace Telemetry.Application.Discussions.Commands.UpdateDiscussion;

public sealed record UpdateDiscussionCommand(
    string Id,
    string Title,
    DiscussionCategory Category,
    IReadOnlyList<string> Labels,
    int Version
) : ICommand<DiscussionDetailsDto>;
