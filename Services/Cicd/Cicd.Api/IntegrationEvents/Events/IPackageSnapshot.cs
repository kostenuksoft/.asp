namespace Cicd.Api.IntegrationEvents.Events;

public interface IPackageSnapshot
{
    long PackageId { get; }

    string PublisherSlug { get; }

    string PackageName { get; }

    string PackageKey { get; }

    decimal LicensePrice { get; }
}
