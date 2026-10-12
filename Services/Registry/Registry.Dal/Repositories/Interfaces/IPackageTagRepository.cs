using Registry.Domain.Entities;

namespace Registry.Dal.Repositories.Interfaces;

public interface IPackageTagRepository : IGenericRepository<PackageTag>
{
    Task<IReadOnlyList<Package>> GetRecentlyTaggedAsync(
        string tagName,
        DateTime since,
        CancellationToken cancellationToken = default
    );
}
