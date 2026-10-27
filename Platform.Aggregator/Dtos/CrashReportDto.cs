namespace Platform.Aggregator.Dtos;

public sealed record CrashReportDto(
    string Id,
    string PackageKey,
    string Kind,
    string? Severity,
    string Fingerprint,
    DateTime OccurredAt
);
