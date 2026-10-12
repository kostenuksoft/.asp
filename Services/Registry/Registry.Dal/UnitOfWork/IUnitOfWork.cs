using Registry.Dal.Repositories.Interfaces;
using Registry.Domain.Entities;

namespace Registry.Dal.UnitOfWork;

public interface IUnitOfWork
{
    IGenericRepository<Publisher> Publishers { get; }

    IPackageRepository Packages { get; }

    ITagRepository Tags { get; }

    IPackageTagRepository PackageTags { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
