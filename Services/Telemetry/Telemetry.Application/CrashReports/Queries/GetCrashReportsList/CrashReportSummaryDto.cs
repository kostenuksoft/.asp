using Telemetry.Domain.Enums;

namespace Telemetry.Application.CrashReports.Queries.GetCrashReportsList;

public sealed record CrashReportSummaryDto
{
    public string Id { get; init; } = string.Empty;

    public CrashReportKind Kind { get; init; }

    public string PackageKey { get; init; } = string.Empty;

    public string VersionKey { get; init; } = string.Empty;

    public string Fingerprint { get; init; } = string.Empty;

    public CrashSeverity? Severity { get; init; }

    public DeploymentEnvironment? Environment { get; init; }

    public DateTime OccurredAt { get; init; }

    public string? DiscussionId { get; init; }

    public int Version { get; init; }
}
