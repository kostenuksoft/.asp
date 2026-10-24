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

public sealed record CrashReportDto(
    string Id,
    string PackageKey,
    string Kind,
    string? Severity,
    string Fingerprint,
    DateTime OccurredAt
);
