using Platform.EventBus;

namespace Registry.Api.IntegrationEvents.Events;

public sealed record PackageLicensePriceChangedEvent(
    long PackageId,
    string PublisherSlug,
    string PackageName,
    string PackageKey,
    decimal LicensePrice,
    decimal PreviousLicensePrice
)
    : IntegrationEvent;
