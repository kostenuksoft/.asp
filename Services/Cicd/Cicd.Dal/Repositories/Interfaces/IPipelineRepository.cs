using Cicd.Domain.Models;

namespace Cicd.Dal.Repositories.Interfaces;

public interface IPipelineRepository : IGenericRepository<Pipeline>
{
    Task<Pipeline?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    );

    Task<int> UpdatePackageKeyAsync(
        long packageId,
        string packageKey,
        string updatedBy,
        CancellationToken cancellationToken = default
    );
}
