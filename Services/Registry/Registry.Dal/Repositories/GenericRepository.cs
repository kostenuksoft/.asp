using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Registry.Dal.Repositories.Interfaces;

namespace Registry.Dal.Repositories;

public class GenericRepository<T>(RegistryDbContext context) : IGenericRepository<T>
    where T : class
{
    protected RegistryDbContext Context { get; } = context;

    protected DbSet<T> Set { get; } = context.Set<T>();

    public async Task<T?> GetByIdAsync(
        object[] keyValues,
        CancellationToken cancellationToken = default
    ) => await Set.FindAsync(keyValues, cancellationToken);

    public async Task<IReadOnlyList<T>> GetAllAsync(
        CancellationToken cancellationToken = default
    ) => await Set.AsNoTracking().ToListAsync(cancellationToken);

    public async Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default
    ) => await Set.AddAsync(entity, cancellationToken);

    public void Update(T entity) => Set.Update(entity);

    public void Delete(T entity) => Set.Remove(entity);

    public async Task<IReadOnlyList<T>> ListAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    ) => await Set.WithSpecification(specification).ToListAsync(cancellationToken);

    public Task<int> CountAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    ) => SpecificationEvaluator.Default.GetQuery(Set, specification, evaluateCriteriaOnly: true).CountAsync(cancellationToken);
}
