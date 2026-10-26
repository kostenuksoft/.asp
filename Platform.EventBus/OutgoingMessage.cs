using System.Diagnostics;
using System.Text.Json;

namespace Platform.EventBus;

public sealed record OutgoingMessage(
    Guid EventId,
    string EventType,
    string Exchange,
    string RoutingKey,
    string? CorrelationId,
    DateTime OccurredAt,
    int SchemaVersion,
    string Payload,
    string? TraceParent
)
{
    public static OutgoingMessage Create(
        IntegrationEvent integrationEvent,
        string exchange,
        string routingKey
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        Type eventType = integrationEvent.GetType();
        return new OutgoingMessage(
            integrationEvent.EventId,
            eventType.Name,
            exchange,
            routingKey,
            integrationEvent.CorrelationId,
            integrationEvent.OccurredAt,
            IntegrationEvent.SchemaVersion,
            JsonSerializer.Serialize(integrationEvent, eventType, EventBusJson.Options),
            Activity.Current?.Id
        );
    }
}
