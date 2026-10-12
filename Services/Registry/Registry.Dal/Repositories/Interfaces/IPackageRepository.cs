using Registry.Domain.Entities;

namespace Registry.Dal.Repositories.Interfaces;

public interface IPackageRepository : IGenericRepository<Package>
{
    Task<Package?> GetWithDetailsAsync(
        long packageId,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsByNameAsync(
        long publisherId,
        string name,
        CancellationToken cancellationToken = default
    );
}
