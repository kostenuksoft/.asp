using MongoDB.Bson;
using MongoDB.Driver;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Exceptions;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Infrastructure.Persistence;

public sealed class DiscussionRepository(MongoDbContext context) : IDiscussionRepository
{
    private readonly IMongoCollection<Discussion> _discussions = context.Discussions;

    public async Task<Discussion?> GetByIdAsync(
        ObjectId id,
        CancellationToken cancellationToken = default
    ) => await _discussions.Find(discussion => discussion.Id == id).FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(
        ObjectId id,
        CancellationToken cancellationToken = default
    ) => _discussions.Find(discussion => discussion.Id == id).AnyAsync(cancellationToken);

    public Task<bool> ExistsAsync(
        string packageKey,
        string title,
        CancellationToken cancellationToken = default
    ) => _discussions
        .Find(discussion => discussion.PackageKey == packageKey && discussion.Title == title)
        .AnyAsync(cancellationToken);

    public Task AddAsync(
        Discussion discussion,
        CancellationToken cancellationToken = default
    ) => _discussions.InsertOneAsync(discussion, cancellationToken: cancellationToken);

    public async Task UpdateAsync(
        Discussion discussion,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(discussion);

        int expectedVersion = discussion.Version;
        discussion.BumpVersion();

        ReplaceOneResult result = await _discussions.ReplaceOneAsync(
            stored => stored.Id == discussion.Id && stored.Version == expectedVersion,
            discussion,
            cancellationToken: cancellationToken
        );

        if (result.MatchedCount == 0)
        {
            throw new ConflictException($"Discussion {discussion.Id} was modified by someone else.");
        }
    }

    public async Task DeleteAsync(
        Discussion discussion,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(discussion);

        DeleteResult result = await _discussions.DeleteOneAsync(
            stored => stored.Id == discussion.Id && stored.Version == discussion.Version,
            cancellationToken
        );

        if (result.DeletedCount == 0)
        {
            throw new ConflictException($"Discussion {discussion.Id} was modified by someone else.");
        }
    }
}
