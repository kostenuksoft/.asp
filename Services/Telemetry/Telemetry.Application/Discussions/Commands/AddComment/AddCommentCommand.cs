using Telemetry.Application.Common.Messaging;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;

namespace Telemetry.Application.Discussions.Commands.AddComment;

public sealed record AddCommentCommand(
    string Id,
    string Author,
    string Body,
    int Version
) : ICommand<DiscussionDetailsDto>;
