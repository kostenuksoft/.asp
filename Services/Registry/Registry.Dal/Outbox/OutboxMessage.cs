namespace Registry.Dal.Outbox;

public sealed class OutboxMessage(
    Guid eventId,
    string eventType,
    string exchange,
    string routingKey,
    string? correlationId,
    DateTime occurredAt,
    int schemaVersion,
    string payload,
    string? traceParent
)
{
    private const int MaxErrorLength = 1000;

    public long OutboxMessageId { get; private set; }

    public Guid EventId { get; private set; } = eventId;

    public string EventType { get; private set; } = eventType;

    public string Exchange { get; private set; } = exchange;

    public string RoutingKey { get; private set; } = routingKey;

    public string? CorrelationId { get; private set; } = correlationId;

    public DateTime OccurredAt { get; private set; } = occurredAt;

    public int SchemaVersion { get; private set; } = schemaVersion;

    public string Payload { get; private set; } = payload;

    public string? TraceParent { get; private set; } = traceParent;

    public DateTime? PublishedAt { get; private set; }

    public int Attempts { get; private set; }

    public string? LastError { get; private set; }

    public void MarkPublished(DateTime publishedAt)
    {
        Attempts++;
        PublishedAt = publishedAt;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Attempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }
}
