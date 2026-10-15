using Telemetry.Domain.Enums;

namespace Telemetry.Application.CrashReports.Queries.GetCrashReportById;

public sealed record CrashReportDto
{
    public string Id { get; init; } = string.Empty;

    public CrashReportKind Kind { get; init; }

    public long PackageVersionId { get; init; }

    public string VersionKey { get; init; } = string.Empty;

    public long PackageId { get; init; }

    public string PackageKey { get; init; } = string.Empty;

    public string? DiscussionId { get; init; }

    public DateTime OccurredAt { get; init; }

    public DateTime ExpiresAt { get; init; }

    public string Fingerprint { get; init; } = string.Empty;

    public DeploymentEnvironment? Environment { get; init; }

    public CrashSeverity? Severity { get; init; }

    public ExceptionInfoDto? Exception { get; init; }

    public IReadOnlyList<StackFrameDto>? StackFrames { get; init; }

    public PerformanceMetricDto? Metric { get; init; }

    public string? CheckName { get; init; }

    public IReadOnlyDictionary<string, string>? Context { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public int Version { get; init; }
}
