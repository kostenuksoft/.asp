namespace Telemetry.Application.PackageCopies.Commands.SyncPackageName;

public sealed record SyncPackageNameResult(
    bool Duplicate,
    long DiscussionsUpdated,
    long InsightsUpdated
);
