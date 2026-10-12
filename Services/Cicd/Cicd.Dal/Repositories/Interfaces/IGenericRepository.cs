namespace Cicd.Dal.Repositories.Interfaces;

public interface IGenericRepository<T>
    where T : class
{
    Task<T?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<long> AddAsync(
        T entity,
        CancellationToken cancellationToken = default
    );

    Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default
    );
}
