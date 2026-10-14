using MongoDB.Bson;
using MongoDB.Driver;
using Telemetry.Application.Common.Interfaces;
using Telemetry.Application.Common.Models;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Application.Discussions.Queries.GetDiscussionsList;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;

namespace Telemetry.Infrastructure.Persistence;

public sealed class DiscussionQueries(MongoDbContext context) : IDiscussionQueries
{
    private static readonly BsonDocument s_summaryFields = new()
    {
        { "_id", new BsonDocument("$toString", "$_id") },
        { "packageId", 1 },
        { "packageKey", 1 },
        { "packageName", 1 },
        { "title", 1 },
        { "category", 1 },
        { "state", 1 },
        { "labels", 1 },
        { "openedBy", 1 },
        { "openedAt", 1 },
        { "commentCount", 1 },
        { "version", 1 },
    };

    private static readonly BsonDocument s_detailsFields = new()
    {
        { "_id", new BsonDocument("$toString", "$_id") },
        { "packageId", 1 },
        { "packageKey", 1 },
        { "packageName", 1 },
        { "publisherSlug", 1 },
        { "title", 1 },
        { "category", 1 },
        { "state", 1 },
        { "labels", 1 },
        { "affectedVersions", 1 },
        { "linkedBuildRunIds", 1 },
        { "openedBy", 1 },
        { "openedAt", 1 },
        { "closedAt", 1 },
        { "commentCount", 1 },
        { "comments", 1 },
        { "createdAt", 1 },
        { "updatedAt", 1 },
        { "version", 1 },
    };

    public async Task<DiscussionDetailsDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken
    )
    {
        ObjectId objectId = ObjectId.Parse(id);

        return await context.Discussions
            .Find(discussion => discussion.Id == objectId)
            .Project<DiscussionDetailsDto>(s_detailsFields)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<DiscussionSummaryDto>> GetListAsync(
        string? packageKey,
        DiscussionState? state,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        FilterDefinitionBuilder<Discussion> filters = Builders<Discussion>.Filter;
        FilterDefinition<Discussion> filter = filters.Empty;

        if (!string.IsNullOrWhiteSpace(packageKey))
        {
            filter &= filters.Eq(discussion => discussion.PackageKey, packageKey);
        }

        if (state is not null)
        {
            filter &= filters.Eq(discussion => discussion.State, state.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            filter &= filters.Text(search);
        }

        long totalCount = await context.Discussions.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<DiscussionSummaryDto> items = await context.Discussions
            .Aggregate()
            .Match(filter)
            .SortByDescending(discussion => discussion.OpenedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<DiscussionSummaryDto>(s_summaryFields)
            .ToListAsync(cancellationToken);

        return new PagedResult<DiscussionSummaryDto>(items, (int)totalCount, page, pageSize);
    }
}
