using MongoDB.Bson;
using MongoDB.Driver;
using Telemetry.Application.Common.Interfaces;
using Telemetry.Application.Common.Models;
using Telemetry.Application.CrashReports.Queries.GetCrashReportById;
using Telemetry.Application.CrashReports.Queries.GetCrashReportsList;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;

namespace Telemetry.Infrastructure.Persistence;

public sealed class CrashReportQueries(MongoDbContext context) : ICrashReportQueries
{
    private static readonly BsonDocument s_summaryFields = new()
    {
        { "_id", new BsonDocument("$toString", "$_id") },
        { "kind", 1 },
        { "packageKey", 1 },
        { "versionKey", 1 },
        { "fingerprint", 1 },
        { "severity", 1 },
        { "environment", 1 },
        { "occurredAt", 1 },
        { "discussionId", new BsonDocument("$toString", "$discussionId") },
        { "version", 1 },
    };

    private static readonly BsonDocument s_detailsFields = new()
    {
        { "_id", new BsonDocument("$toString", "$_id") },
        { "kind", 1 },
        { "packageVersionId", 1 },
        { "versionKey", 1 },
        { "packageId", 1 },
        { "packageKey", 1 },
        { "discussionId", new BsonDocument("$toString", "$discussionId") },
        { "occurredAt", 1 },
        { "expiresAt", 1 },
        { "fingerprint", 1 },
        { "environment", 1 },
        { "severity", 1 },
        { "exception", 1 },
        { "stackFrames", 1 },
        { "metric", 1 },
        { "checkName", 1 },
        { "context", 1 },
        { "createdAt", 1 },
        { "updatedAt", 1 },
        { "version", 1 },
    };

    public async Task<CrashReportDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken
    )
    {
        ObjectId objectId = ObjectId.Parse(id);

        return await context.CrashReports
            .Find(report => report.Id == objectId)
            .Project<CrashReportDto>(s_detailsFields)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<CrashReportSummaryDto>> GetListAsync(
        string? packageKey,
        CrashReportKind? kind,
        CrashSeverity? severity,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        FilterDefinitionBuilder<CrashReport> filters = Builders<CrashReport>.Filter;
        FilterDefinition<CrashReport> filter = filters.Empty;

        if (!string.IsNullOrWhiteSpace(packageKey))
        {
            filter &= filters.Eq(report => report.PackageKey, packageKey);
        }

        if (kind is not null)
        {
            filter &= filters.Eq(report => report.Kind, kind.Value);
        }

        if (severity is not null)
        {
            filter &= filters.Eq(report => report.Severity, severity);
        }

        long totalCount = await context.CrashReports.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<CrashReportSummaryDto> items = await context.CrashReports
            .Aggregate()
            .Match(filter)
            .SortByDescending(report => report.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .Project<CrashReportSummaryDto>(s_summaryFields)
            .ToListAsync(cancellationToken);

        return new PagedResult<CrashReportSummaryDto>(items, (int)totalCount, page, pageSize);
    }
}
