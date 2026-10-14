using MongoDB.Bson;
using Telemetry.Domain.Common;
using Telemetry.Domain.Enums;
using Telemetry.Domain.Exceptions;
using Telemetry.Domain.ValueObjects;

namespace Telemetry.Domain.Entities;

public sealed class CrashReport : BaseEntity
{
    public const int MaxFingerprintLength = 64;
    public const int MaxStackFrames = 200;

    public static readonly TimeSpan Retention = TimeSpan.FromDays(365);

    private static readonly TimeSpan s_clockSkew = TimeSpan.FromMinutes(5);

    private CrashReport()
    {
    }

    private CrashReport(
        CrashReportKind kind,
        long packageVersionId,
        string versionKey,
        long packageId,
        string packageKey,
        string fingerprint,
        DateTime occurredAt,
        DeploymentEnvironment? environment,
        CrashSeverity? severity,
        IReadOnlyDictionary<string, string>? context,
        string createdBy
    )
    {
        if (packageVersionId <= 0 || packageId <= 0)
        {
            throw new DomainException("A crash report needs the Ids of an existing package and version.");
        }

        if (!IsVersionOf(versionKey, packageKey))
        {
            throw new DomainException("Version key must be '<package key>@<semver>' of the same package.");
        }

        if (string.IsNullOrWhiteSpace(fingerprint) || fingerprint.Length > MaxFingerprintLength)
        {
            throw new DomainException($"Fingerprint is required and has at most {MaxFingerprintLength} characters.");
        }

        if (occurredAt > DateTime.UtcNow + s_clockSkew)
        {
            throw new DomainException("A crash cannot occur in the future.");
        }

        Kind = kind;
        PackageVersionId = packageVersionId;
        VersionKey = versionKey;
        PackageId = packageId;
        PackageKey = packageKey;
        Fingerprint = fingerprint;
        OccurredAt = occurredAt;
        ExpiresAt = occurredAt + Retention;
        Environment = environment;
        Severity = severity;
        Context = context;
        CreatedBy = createdBy;
    }

    public CrashReportKind Kind { get; private set; }

    public long PackageVersionId { get; private set; }

    public string VersionKey { get; private set; } = string.Empty;

    public long PackageId { get; private set; }

    public string PackageKey { get; private set; } = string.Empty;

    public ObjectId? DiscussionId { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public string Fingerprint { get; private set; } = string.Empty;

    public DeploymentEnvironment? Environment { get; private set; }

    public CrashSeverity? Severity { get; private set; }

    public ExceptionInfo? Exception { get; private set; }

    public IReadOnlyList<StackFrame>? StackFrames { get; private set; }

    public PerformanceMetric? Metric { get; private set; }

    public string? CheckName { get; private set; }

    public IReadOnlyDictionary<string, string>? Context { get; private set; }

    public static CrashReport ForUnhandledException(
        long packageVersionId,
        string versionKey,
        long packageId,
        string packageKey,
        string fingerprint,
        DateTime occurredAt,
        DeploymentEnvironment? environment,
        CrashSeverity? severity,
        IReadOnlyDictionary<string, string>? context,
        string createdBy,
        ExceptionInfo exception,
        IReadOnlyCollection<StackFrame> stackFrames
    )
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(stackFrames);

        if (stackFrames.Count is 0 or > MaxStackFrames)
        {
            throw new DomainException($"An exception report needs 1 to {MaxStackFrames} stack frames.");
        }

        CrashReport report = new(
            CrashReportKind.UnhandledException,
            packageVersionId,
            versionKey,
            packageId,
            packageKey,
            fingerprint,
            occurredAt,
            environment,
            severity,
            context,
            createdBy
        );
        report.Exception = exception;
        report.StackFrames = [.. stackFrames];
        return report;
    }

    public static CrashReport ForPerformanceDegradation(
        long packageVersionId,
        string versionKey,
        long packageId,
        string packageKey,
        string fingerprint,
        DateTime occurredAt,
        DeploymentEnvironment? environment,
        CrashSeverity? severity,
        IReadOnlyDictionary<string, string>? context,
        string createdBy,
        PerformanceMetric metric
    )
    {
        ArgumentNullException.ThrowIfNull(metric);

        CrashReport report = new(
            CrashReportKind.PerformanceDegradation,
            packageVersionId,
            versionKey,
            packageId,
            packageKey,
            fingerprint,
            occurredAt,
            environment,
            severity,
            context,
            createdBy
        );
        report.Metric = metric;
        return report;
    }

    public static CrashReport ForHealthCheckFailure(
        long packageVersionId,
        string versionKey,
        long packageId,
        string packageKey,
        string fingerprint,
        DateTime occurredAt,
        DeploymentEnvironment? environment,
        CrashSeverity? severity,
        IReadOnlyDictionary<string, string>? context,
        string createdBy,
        string checkName
    )
    {
        if (string.IsNullOrWhiteSpace(checkName))
        {
            throw new DomainException("A health check failure needs the name of the check.");
        }

        CrashReport report = new(
            CrashReportKind.HealthCheckFailure,
            packageVersionId,
            versionKey,
            packageId,
            packageKey,
            fingerprint,
            occurredAt,
            environment,
            severity,
            context,
            createdBy
        );
        report.CheckName = checkName;
        return report;
    }

    public void LinkToDiscussion(ObjectId discussionId)
    {
        if (DiscussionId is not null)
        {
            throw new DomainException("The crash report is already linked to a discussion.");
        }

        DiscussionId = discussionId;
        Touch();
    }

    private static bool IsVersionOf(
        string versionKey,
        string packageKey
    ) => !string.IsNullOrWhiteSpace(packageKey)
        && versionKey.StartsWith($"{packageKey}@", StringComparison.Ordinal)
        && versionKey.Length > packageKey.Length + 1;
}
