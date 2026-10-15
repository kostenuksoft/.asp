using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Telemetry.Domain.Entities;
using Telemetry.Infrastructure.Persistence;

namespace Telemetry.Infrastructure.Seeding;

public sealed class CrashReportSeeder(
    MongoDbContext context,
    ILogger<CrashReportSeeder> logger
)
    : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.CrashReports.EstimatedDocumentCountAsync(cancellationToken: cancellationToken) > 0)
        {
            logger.LogInformation("Crash reports already present, seeding skipped");
            return;
        }

        IReadOnlyList<CrashReport> reports = SeedData.Load<CrashReport>("crashReports.json");
        await context.CrashReports.InsertManyAsync(reports, cancellationToken: cancellationToken);
        logger.LogInformation("Seeded {Count} crash reports", reports.Count);
    }
}
