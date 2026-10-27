using Microsoft.EntityFrameworkCore;
using Registry.Dal.Repositories.Interfaces;
using Registry.Domain.Entities;

namespace Registry.Dal.Repositories;

public sealed class PackageRepository(RegistryDbContext context)
    : GenericRepository<Package>(context), IPackageRepository
{
    public Task<Package?> GetWithDetailsAsync(
        long packageId,
        CancellationToken cancellationToken = default
    ) => Set
        .Include(package => package.Publisher)
        .Include(package => package.Versions).ThenInclude(version => version.Manifest)
        .Include(package => package.PackageTags).ThenInclude(packageTag => packageTag.Tag)
        .AsSingleQuery()
        .AsNoTracking()
        .FirstOrDefaultAsync(package => package.PackageId == packageId, cancellationToken);

    public Task<Package?> GetWithPublisherAsync(
        long packageId,
        CancellationToken cancellationToken = default
    ) => Set
        .Include(package => package.Publisher)
        .FirstOrDefaultAsync(package => package.PackageId == packageId, cancellationToken);

    public Task<bool> ExistsByNameAsync(
        long publisherId,
        string name,
        CancellationToken cancellationToken = default
    ) => Set
        .IgnoreQueryFilters()
        .AnyAsync(package => package.PublisherId == publisherId && package.Name == name, cancellationToken);
}
