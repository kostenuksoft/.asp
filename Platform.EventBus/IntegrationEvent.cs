namespace Platform.EventBus;

public abstract record IntegrationEvent
{
    public const int SchemaVersion = 1;

    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public string? CorrelationId { get; init; }
}
