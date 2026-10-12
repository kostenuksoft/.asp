using Cicd.Domain.Models;

namespace Cicd.Dal.Repositories.Interfaces;

public interface IPipelineRepository : IGenericRepository<Pipeline>
{
    Task<Pipeline?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
