using Ardalis.Specification;

namespace Registry.Dal.Repositories.Interfaces;

public interface IGenericRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(
        object[] keyValues,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(
        T entity,
        CancellationToken cancellationToken = default
    );

    void Update(T entity);

    void Delete(T entity);

    Task<IReadOnlyList<T>> ListAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );

    Task<int> CountAsync(
        ISpecification<T> specification,
        CancellationToken cancellationToken = default
    );
}
