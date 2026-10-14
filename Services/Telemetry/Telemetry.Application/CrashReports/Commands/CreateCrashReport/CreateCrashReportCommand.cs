using Telemetry.Application.Common.Messaging;
using Telemetry.Application.CrashReports.Queries.GetCrashReportById;
using Telemetry.Domain.Enums;

namespace Telemetry.Application.CrashReports.Commands.CreateCrashReport;

public sealed record CreateCrashReportCommand(
    CrashReportKind Kind,
    long PackageVersionId,
    string VersionKey,
    long PackageId,
    string PackageKey,
    string Fingerprint,
    DateTime OccurredAt,
    DeploymentEnvironment? Environment,
    CrashSeverity? Severity,
    IReadOnlyDictionary<string, string>? Context,
    ExceptionInfoDto? Exception,
    IReadOnlyList<StackFrameDto>? StackFrames,
    PerformanceMetricDto? Metric,
    string? CheckName,
    string? DiscussionId
) : ICommand<CrashReportDto>;
