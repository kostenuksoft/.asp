using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Telemetry.Domain.Entities;
using Telemetry.Infrastructure.Persistence;

namespace Telemetry.Infrastructure.Seeding;

public sealed class DiscussionSeeder(
    MongoDbContext context,
    ILogger<DiscussionSeeder> logger
)
    : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.Discussions.EstimatedDocumentCountAsync(cancellationToken: cancellationToken) > 0)
        {
            logger.LogInformation("Discussions already present, seeding skipped");
            return;
        }

        IReadOnlyList<Discussion> discussions = SeedData.Load<Discussion>("discussions.json");
        await context.Discussions.InsertManyAsync(discussions, cancellationToken: cancellationToken);
        logger.LogInformation("Seeded {Count} discussions", discussions.Count);
    }
}
