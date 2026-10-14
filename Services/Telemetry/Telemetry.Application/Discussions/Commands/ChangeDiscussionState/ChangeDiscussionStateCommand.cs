using Telemetry.Application.Common.Messaging;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Domain.Enums;

namespace Telemetry.Application.Discussions.Commands.ChangeDiscussionState;

public sealed record ChangeDiscussionStateCommand(
    string Id,
    DiscussionState State,
    int Version
) : ICommand<DiscussionDetailsDto>;
