using Platform.EventBus;

namespace Registry.Api.IntegrationEvents.Events;

public sealed record PackageDeletedEvent(
    long PackageId,
    string PublisherSlug,
    string PackageName,
    string PackageKey,
    decimal LicensePrice
)
    : IntegrationEvent;
