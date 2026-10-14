using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Telemetry.Domain.Entities;
using Telemetry.Infrastructure.Persistence;

namespace Telemetry.Infrastructure.Seeding;

public sealed class IndexCreationService(
    MongoDbContext context,
    ILogger<IndexCreationService> logger
)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IndexKeysDefinitionBuilder<Discussion> discussionKeys = Builders<Discussion>.IndexKeys;
        IEnumerable<string> discussionIndexes = await context.Discussions.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<Discussion>(
                    discussionKeys
                        .Ascending(discussion => discussion.PackageKey)
                        .Ascending(discussion => discussion.Title),
                    new CreateIndexOptions { Name = "ux_packageKey_title", Unique = true }
                ),
                new CreateIndexModel<Discussion>(
                    discussionKeys
                        .Ascending(discussion => discussion.PackageKey)
                        .Ascending(discussion => discussion.State)
                        .Descending(discussion => discussion.OpenedAt),
                    new CreateIndexOptions { Name = "ix_packageKey_state_openedAt" }
                ),
                new CreateIndexModel<Discussion>(
                    discussionKeys.Ascending(discussion => discussion.Labels),
                    new CreateIndexOptions { Name = "ix_labels" }
                ),
                new CreateIndexModel<Discussion>(
                    discussionKeys.Text(discussion => discussion.Title).Text("comments.body"),
                    new CreateIndexOptions { Name = "tx_title_commentBody" }
                ),
            ],
            cancellationToken
        );

        IndexKeysDefinitionBuilder<CrashReport> reportKeys = Builders<CrashReport>.IndexKeys;
        IEnumerable<string> reportIndexes = await context.CrashReports.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<CrashReport>(
                    reportKeys.Ascending(report => report.Fingerprint).Descending(report => report.OccurredAt),
                    new CreateIndexOptions { Name = "ix_fingerprint_occurredAt" }
                ),
                new CreateIndexModel<CrashReport>(
                    reportKeys.Ascending(report => report.VersionKey).Ascending(report => report.Kind),
                    new CreateIndexOptions { Name = "ix_versionKey_kind" }
                ),
                new CreateIndexModel<CrashReport>(
                    reportKeys.Ascending(report => report.Severity).Descending(report => report.OccurredAt),
                    new CreateIndexOptions { Name = "ix_severity_occurredAt" }
                ),
                new CreateIndexModel<CrashReport>(
                    reportKeys.Ascending(report => report.DiscussionId),
                    new CreateIndexOptions { Name = "ix_discussionId", Sparse = true }
                ),
                new CreateIndexModel<CrashReport>(
                    reportKeys.Ascending(report => report.ExpiresAt),
                    new CreateIndexOptions { Name = "ix_expiresAt_ttl", ExpireAfter = TimeSpan.Zero }
                ),
            ],
            cancellationToken
        );

        logger.LogInformation(
            "MongoDB indexes ensured: {DiscussionIndexes}; {CrashReportIndexes}",
            discussionIndexes,
            reportIndexes
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
