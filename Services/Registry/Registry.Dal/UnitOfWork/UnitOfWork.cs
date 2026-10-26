using Registry.Dal.Repositories;
using Registry.Dal.Repositories.Interfaces;
using Registry.Domain.Entities;

namespace Registry.Dal.UnitOfWork;

public sealed class UnitOfWork(RegistryDbContext context) : IUnitOfWork
{
    public IGenericRepository<Publisher> Publishers { get; } = new GenericRepository<Publisher>(context);

    public IPackageRepository Packages { get; } = new PackageRepository(context);

    public ITagRepository Tags { get; } = new TagRepository(context);

    public IPackageTagRepository PackageTags { get; } = new PackageTagRepository(context);

    public IOutboxRepository Outbox { get; } = new OutboxRepository(context);

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default
    ) => context.SaveChangesAsync(cancellationToken);

    public Task BeginTransactionAsync(
        CancellationToken cancellationToken = default
    ) => context.Database.BeginTransactionAsync(cancellationToken);

    public Task CommitAsync(
        CancellationToken cancellationToken = default
    ) => context.Database.CommitTransactionAsync(cancellationToken);

    public Task RollbackAsync(
        CancellationToken cancellationToken = default
    ) => context.Database.CurrentTransaction is null
        ? Task.CompletedTask
        : context.Database.RollbackTransactionAsync(cancellationToken);
}
