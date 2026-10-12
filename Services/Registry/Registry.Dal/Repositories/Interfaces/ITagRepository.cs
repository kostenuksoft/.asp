using Registry.Domain.Entities;

namespace Registry.Dal.Repositories.Interfaces;

public interface ITagRepository : IGenericRepository<Tag>
{
    Task<Tag?> GetWithPackagesAsync(
        long tagId,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    );
}
