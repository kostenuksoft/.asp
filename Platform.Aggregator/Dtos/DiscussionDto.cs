namespace Platform.Aggregator.Dtos;

public sealed record DiscussionDto(
    string Id,
    string PackageKey,
    string Title,
    string Category,
    string State,
    int CommentCount,
    DateTime OpenedAt
);
