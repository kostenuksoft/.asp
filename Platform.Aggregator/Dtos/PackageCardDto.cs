namespace Platform.Aggregator.Dtos;

public sealed record PackageCardDto(
    long PackageId,
    string PackageKey,
    IReadOnlyList<string> BuiltVersions,
    string? Description,
    decimal? LicensePrice,
    long? DownloadCount,
    bool? IsListed,
    int? DiscussionCount,
    IReadOnlyList<DiscussionDto>? Discussions,
    int? CrashReportCount,
    IReadOnlyList<CrashReportDto>? CrashReports
);
