using Cicd.Domain.Models;

namespace Cicd.Dal.Repositories.Interfaces;

public interface IArtifactRepository : IGenericRepository<Artifact>
{
    Task<Artifact?> GetBySha256Async(string sha256, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Artifact>> GetByPackageAsync(long packageId, CancellationToken cancellationToken = default);

    Task LinkToRunAsync(ArtifactUsage usage, CancellationToken cancellationToken = default);
}
