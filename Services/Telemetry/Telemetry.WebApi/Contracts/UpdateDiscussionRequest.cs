using Telemetry.Domain.Enums;

namespace Telemetry.WebApi.Contracts;

public sealed record UpdateDiscussionRequest(
    string Title,
    DiscussionCategory Category,
    IReadOnlyList<string> Labels,
    int Version
);
