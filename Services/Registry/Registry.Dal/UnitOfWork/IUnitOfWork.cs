using Registry.Dal.Repositories.Interfaces;
using Registry.Domain.Entities;

namespace Registry.Dal.UnitOfWork;

public interface IUnitOfWork
{
    IGenericRepository<Publisher> Publishers { get; }

    IPackageRepository Packages { get; }

    ITagRepository Tags { get; }

    IPackageTagRepository PackageTags { get; }

    IOutboxRepository Outbox { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
