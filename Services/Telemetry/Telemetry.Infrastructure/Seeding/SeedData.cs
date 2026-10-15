using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Telemetry.Infrastructure.Seeding;

internal static class SeedData
{
    public static IReadOnlyList<T> Load<T>(string fileName)
    {
        string resourceName = $"{typeof(SeedData).Namespace}.Data.{fileName}";
        using Stream stream = typeof(SeedData).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Seed resource '{resourceName}' is missing.");
        using StreamReader reader = new(stream);

        BsonArray documents = BsonSerializer.Deserialize<BsonArray>(reader.ReadToEnd());
        return [.. documents.Select(document => BsonSerializer.Deserialize<T>(document.AsBsonDocument))];
    }
}
