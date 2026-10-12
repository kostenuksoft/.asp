using Ardalis.Specification;
using Registry.Domain.Entities;

namespace Registry.Bll.Specifications;

public sealed class TopDownloadedPackagesSpec : Specification<Package>
{
    public TopDownloadedPackagesSpec(int take)
    {
        Query
            .Include(package => package.Publisher)
            .Where(package => package.IsListed)
            .OrderByDescending(package => package.DownloadCount)
            .Take(take)
            .AsNoTracking();
    }
}
