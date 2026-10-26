using Platform.EventBus;

namespace Cicd.Api.IntegrationEvents.Events;

public sealed record PackageRenamedEvent(
    long PackageId,
    string PublisherSlug,
    string PackageName,
    string PackageKey,
    decimal LicensePrice,
    string PreviousPackageKey
)
    : IntegrationEvent, IPackageSnapshot;
