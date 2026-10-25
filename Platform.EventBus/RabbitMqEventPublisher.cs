using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Platform.EventBus;

public sealed class RabbitMqEventPublisher(IConnection connection, ILogger<RabbitMqEventPublisher> logger) : IEventPublisher
{
    private static readonly CreateChannelOptions s_confirmedChannel = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true
    );

    public async Task PublishAsync(
        OutgoingMessage message,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(message);

        using Activity? activity = TracePropagation.StartPublishActivity(message);

        BasicProperties properties = new()
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = message.EventId.ToString(),
            Type = message.EventType,
            Timestamp = new AmqpTimestamp(new DateTimeOffset(message.OccurredAt.Ticks, TimeSpan.Zero).ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                [EventBusHeaders.CorrelationId] = message.CorrelationId,
                [EventBusHeaders.SchemaVersion] = message.SchemaVersion,
            },
        };
        TracePropagation.Inject(activity, properties);

        await using IChannel channel = await connection.CreateChannelAsync(s_confirmedChannel, cancellationToken);
        await channel.BasicPublishAsync(
            message.Exchange,
            message.RoutingKey,
            mandatory: true,
            properties,
            Encoding.UTF8.GetBytes(message.Payload),
            cancellationToken
        );

        logger.LogInformation(
            "Published {EventType} {EventId} to {Exchange}/{RoutingKey} with correlation {CorrelationId}",
            message.EventType,
            message.EventId,
            message.Exchange,
            message.RoutingKey,
            message.CorrelationId
        );
    }
}
