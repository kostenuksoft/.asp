using Cicd.Domain.Models;

namespace Cicd.Dal.Repositories.Interfaces;

public interface IPackageRepository
{
    Task<Package?> GetByIdAsync(
        long packageId,
        CancellationToken cancellationToken = default
    );

    Task<bool> UpsertAsync(
        Package package,
        CancellationToken cancellationToken = default
    );
}
