using Registry.Domain.Entities;

namespace Registry.Bll.Services.Interfaces;

public interface IPackageEvents
{
    void PackageCreated(Package package);

    void PackageRenamed(
        Package package,
        string previousName
    );

    void LicensePriceChanged(
        Package package,
        decimal previousPrice
    );

    void PackageDeleted(Package package);

    Task PublishPendingAsync(CancellationToken cancellationToken);
}
