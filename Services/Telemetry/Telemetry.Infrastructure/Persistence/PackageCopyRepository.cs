using MongoDB.Bson;
using MongoDB.Driver;
using Telemetry.Application.Common.Interfaces;
using Telemetry.Domain.Entities;

namespace Telemetry.Infrastructure.Persistence;

public sealed class PackageCopyRepository(MongoDbContext context) : IPackageCopyRepository
{
    public async Task<bool> IsProcessedAsync(
        Guid eventId,
        CancellationToken cancellationToken
    ) => await context.ProcessedEvents
        .Find(Builders<BsonDocument>.Filter.Eq("_id", eventId.ToString()))
        .AnyAsync(cancellationToken);

    public async Task<PackageCopyUpdate> RefreshNameAsync(
        long packageId,
        string publisherSlug,
        string packageName,
        string packageKey,
        DateTime changedAt,
        CancellationToken cancellationToken
    )
    {
        FilterDefinitionBuilder<Discussion> discussionFilter = Builders<Discussion>.Filter;
        UpdateResult discussions = await context.Discussions.UpdateManyAsync(
            discussionFilter.Eq(discussion => discussion.PackageId, packageId)
                & discussionFilter.Not(discussionFilter.Gte(discussion => discussion.PackageSyncedAt, changedAt)),
            Builders<Discussion>.Update
                .Set(discussion => discussion.PackageName, packageName)
                .Set(discussion => discussion.PackageKey, packageKey)
                .Set(discussion => discussion.PublisherSlug, publisherSlug)
                .Set(discussion => discussion.PackageSyncedAt, changedAt)
                .Set(discussion => discussion.UpdatedAt, DateTime.UtcNow)
                .Inc(discussion => discussion.Version, 1),
            cancellationToken: cancellationToken
        );

        FilterDefinitionBuilder<BsonDocument> insightFilter = Builders<BsonDocument>.Filter;
        UpdateResult insights = await context.PackageInsights.UpdateManyAsync(
            insightFilter.Eq("packageId", packageId)
                & insightFilter.Not(insightFilter.Gte("packageSyncedAt", changedAt)),
            Builders<BsonDocument>.Update
                .Set("packageName", packageName)
                .Set("packageKey", packageKey)
                .Set("publisherSlug", publisherSlug)
                .Set("packageSyncedAt", changedAt),
            cancellationToken: cancellationToken
        );

        return new PackageCopyUpdate(discussions.ModifiedCount, insights.ModifiedCount);
    }

    public async Task<bool> MarkProcessedAsync(
        Guid eventId,
        string eventType,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await context.ProcessedEvents.InsertOneAsync(
                new BsonDocument
                {
                    { "_id", eventId.ToString() },
                    { "eventType", eventType },
                    { "processedAt", DateTime.UtcNow },
                },
                cancellationToken: cancellationToken
            );
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}
