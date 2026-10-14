using MongoDB.Driver;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Infrastructure.Persistence;

public sealed class CrashReportRepository(MongoDbContext context) : ICrashReportRepository
{
    private readonly IMongoCollection<CrashReport> _reports = context.CrashReports;

    public Task AddAsync(
        CrashReport report,
        CancellationToken cancellationToken = default
    ) => _reports.InsertOneAsync(report, cancellationToken: cancellationToken);
}
