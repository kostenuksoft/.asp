using Telemetry.Domain.Enums;

namespace Telemetry.WebApi.Contracts;

public sealed record ChangeDiscussionStateRequest(
    DiscussionState State,
    int Version
);
