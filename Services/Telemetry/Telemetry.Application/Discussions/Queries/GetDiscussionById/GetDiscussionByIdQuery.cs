using Telemetry.Application.Common.Messaging;

namespace Telemetry.Application.Discussions.Queries.GetDiscussionById;

public sealed record GetDiscussionByIdQuery(string Id) : IQuery<DiscussionDetailsDto>;
