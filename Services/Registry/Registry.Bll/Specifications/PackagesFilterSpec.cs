using Ardalis.Specification;
using Registry.Bll.Dtos;
using Registry.Domain.Entities;

namespace Registry.Bll.Specifications;

public sealed class PackagesFilterSpec : Specification<Package>
{
    public PackagesFilterSpec(
        PackageQuery query,
        int pageSize
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        Query.Include(package => package.Publisher).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            Query.Where(package => package.Name.Contains(query.Search));
        }

        if (query.PublisherId is not null)
        {
            Query.Where(package => package.PublisherId == query.PublisherId);
        }

        if (query.MinPrice is not null)
        {
            Query.Where(package => package.LicensePrice >= query.MinPrice);
        }

        if (query.MaxPrice is not null)
        {
            Query.Where(package => package.LicensePrice <= query.MaxPrice);
        }

        bool descending = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        switch (query.SortBy.ToUpperInvariant(), descending)
        {
            case ("PRICE", true):
                Query.OrderByDescending(package => package.LicensePrice).ThenBy(package => package.PackageId);
                break;
            case ("PRICE", false):
                Query.OrderBy(package => package.LicensePrice).ThenBy(package => package.PackageId);
                break;
            case ("DOWNLOADS", true):
                Query.OrderByDescending(package => package.DownloadCount).ThenBy(package => package.PackageId);
                break;
            case ("DOWNLOADS", false):
                Query.OrderBy(package => package.DownloadCount).ThenBy(package => package.PackageId);
                break;
            case (_, true):
                Query.OrderByDescending(package => package.Name).ThenBy(package => package.PackageId);
                break;
            default:
                Query.OrderBy(package => package.Name).ThenBy(package => package.PackageId);
                break;
        }

        Query.Skip((query.Page - 1) * pageSize).Take(pageSize);
    }
}
