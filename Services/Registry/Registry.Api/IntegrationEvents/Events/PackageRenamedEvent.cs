using Platform.EventBus;

namespace Registry.Api.IntegrationEvents.Events;

public sealed record PackageRenamedEvent(
    long PackageId,
    string PublisherSlug,
    string PackageName,
    string PackageKey,
    decimal LicensePrice,
    string PreviousName,
    string PreviousPackageKey
)
    : IntegrationEvent;
