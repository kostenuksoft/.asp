namespace Platform.Aggregator.Dtos;

public sealed record BuildRunCardDto(
    BuildRunSummaryDto Run,
    IReadOnlyList<PackageCardDto> Packages,
    IReadOnlyList<string> Warnings
);

public sealed record BuildRunSummaryDto(
    long BuildRunId,
    string PipelineName,
    int RunNumber,
    string Status,
    string BranchName,
    string CommitSha,
    DateTime QueuedAt,
    DateTime? FinishedAt,
    int? DurationSeconds
);

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
