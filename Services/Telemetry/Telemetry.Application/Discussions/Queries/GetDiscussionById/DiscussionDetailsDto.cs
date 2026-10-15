using Telemetry.Domain.Enums;

namespace Telemetry.Application.Discussions.Queries.GetDiscussionById;

public sealed record DiscussionDetailsDto
{
    public string Id { get; init; } = string.Empty;

    public long PackageId { get; init; }

    public string PackageKey { get; init; } = string.Empty;

    public string PackageName { get; init; } = string.Empty;

    public string PublisherSlug { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DiscussionCategory Category { get; init; }

    public DiscussionState State { get; init; }

    public IReadOnlyList<string> Labels { get; init; } = [];

    public IReadOnlyList<string> AffectedVersions { get; init; } = [];

    public IReadOnlyList<long> LinkedBuildRunIds { get; init; } = [];

    public string OpenedBy { get; init; } = string.Empty;

    public DateTime OpenedAt { get; init; }

    public DateTime? ClosedAt { get; init; }

    public int CommentCount { get; init; }

    public IReadOnlyList<CommentDto> Comments { get; init; } = [];

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public int Version { get; init; }
}
