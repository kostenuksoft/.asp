using MongoDB.Bson;
using Telemetry.Domain.Entities;

namespace Telemetry.Domain.Interfaces;

public interface IDiscussionRepository
{
    Task<Discussion?> GetByIdAsync(
        ObjectId id,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsAsync(
        ObjectId id,
        CancellationToken cancellationToken = default
    );

    Task<bool> ExistsAsync(
        string packageKey,
        string title,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(
        Discussion discussion,
        CancellationToken cancellationToken = default
    );

    Task UpdateAsync(
        Discussion discussion,
        CancellationToken cancellationToken = default
    );

    Task DeleteAsync(
        Discussion discussion,
        CancellationToken cancellationToken = default
    );
}
