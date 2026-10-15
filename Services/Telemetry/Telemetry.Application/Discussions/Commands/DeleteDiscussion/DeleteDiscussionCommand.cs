using Telemetry.Application.Common.Messaging;

namespace Telemetry.Application.Discussions.Commands.DeleteDiscussion;

public sealed record DeleteDiscussionCommand(
    string Id,
    int Version
) : ICommand;
