using Platform.EventBus;

namespace Telemetry.WebApi.IntegrationEvents.Events;

public sealed record PackageRenamedEvent(
    long PackageId,
    string PublisherSlug,
    string PackageName,
    string PackageKey,
    string PreviousPackageKey
)
    : IntegrationEvent;
