using Microsoft.EntityFrameworkCore;
using Registry.Dal.Repositories.Interfaces;
using Registry.Domain.Entities;

namespace Registry.Dal.Repositories;

public sealed class PackageTagRepository(RegistryDbContext context) : GenericRepository<PackageTag>(context), IPackageTagRepository
{
    public async Task<IReadOnlyList<Package>> GetRecentlyTaggedAsync(
        string tagName,
        DateTime since,
        CancellationToken cancellationToken = default
    ) => await Set
        .Where(packageTag => packageTag.Tag.Name == tagName && packageTag.AssignedAt >= since)
        .OrderByDescending(packageTag => packageTag.AssignedAt)
        .Select(packageTag => packageTag.Package)
        .AsNoTracking()
        .ToListAsync(cancellationToken);
}
