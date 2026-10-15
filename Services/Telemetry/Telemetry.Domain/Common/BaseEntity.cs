using MongoDB.Bson;

namespace Telemetry.Domain.Common;

public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; } = ObjectId.GenerateNewId();

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    public string? CreatedBy { get; protected set; }

    public DateTime? UpdatedAt { get; protected set; }

    public int Version { get; protected set; }

    public void BumpVersion() => Version++;

    protected void Touch() => UpdatedAt = DateTime.UtcNow;
}
