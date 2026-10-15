using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Telemetry.Domain.Entities;

namespace Telemetry.Infrastructure.Persistence;

public sealed class MongoDbContext(IOptions<MongoDbOptions> options) : IDisposable
{
    public IMongoDatabase Database { get; } = new MongoClient(options.Value.ConnectionString)
        .GetDatabase(options.Value.DatabaseName);

    public IMongoCollection<Discussion> Discussions => Database.GetCollection<Discussion>("discussions");

    public IMongoCollection<CrashReport> CrashReports => Database.GetCollection<CrashReport>("crashReports");

    public void Dispose() => Database.Client.Dispose();
}
