using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using Telemetry.Domain.Entities;

namespace Telemetry.Infrastructure.Persistence;

public sealed class MongoDbContext(IOptions<MongoDbOptions> options) : IDisposable
{
    public IMongoDatabase Database { get; } = CreateClient(options.Value.ConnectionString)
        .GetDatabase(options.Value.DatabaseName);

    public IMongoCollection<Discussion> Discussions => Database.GetCollection<Discussion>("discussions");

    public IMongoCollection<CrashReport> CrashReports => Database.GetCollection<CrashReport>("crashReports");

    public IMongoCollection<BsonDocument> PackageInsights => Database.GetCollection<BsonDocument>("packageInsights");

    public IMongoCollection<BsonDocument> ProcessedEvents => Database.GetCollection<BsonDocument>("processedEvents");

    public void Dispose() => Database.Client.Dispose();

    private static MongoClient CreateClient(string connectionString)
    {
        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ClusterConfigurator = cluster => cluster.Subscribe(new DiagnosticsActivityEventSubscriber());
        return new MongoClient(settings);
    }
}
