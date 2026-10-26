using Telemetry.Application.Common.Messaging;

namespace Telemetry.Application.PackageCopies.Commands.SyncPackageName;

public sealed record SyncPackageNameCommand(
    Guid EventId,
    string EventType,
    long PackageId,
    string PublisherSlug,
    string PackageName,
    string PackageKey,
    DateTime OccurredAt
) : ICommand<SyncPackageNameResult>;
