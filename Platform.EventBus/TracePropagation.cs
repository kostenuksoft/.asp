using System.Diagnostics;
using System.Text;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus;

public static class TracePropagation
{
    public const string ActivitySourceName = "Platform.Messaging";

    public static readonly ActivitySource Source = new(ActivitySourceName);

    private static TextMapPropagator Propagator => Propagators.DefaultTextMapPropagator;

    public static Activity? StartPublishActivity(OutgoingMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        ActivityContext parent = ActivityContext.TryParse(message.TraceParent, null, out ActivityContext stored)
            ? stored
            : default;
        Activity? activity = Source.StartActivity($"{message.Exchange} publish", ActivityKind.Producer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("messaging.destination.name", message.Exchange);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", message.RoutingKey);
        activity?.SetTag("messaging.message.id", message.EventId.ToString());
        activity?.SetTag("event.type", message.EventType);
        activity?.SetTag("correlation.id", message.CorrelationId);
        return activity;
    }

    public static void Inject(
        Activity? activity,
        BasicProperties properties
    )
    {
        ArgumentNullException.ThrowIfNull(properties);

        if (activity is null)
        {
            return;
        }

        properties.Headers ??= new Dictionary<string, object?>();
        Propagator.Inject(
            new PropagationContext(activity.Context, Baggage.Current),
            properties.Headers,
            static (headers, key, value) => headers[key] = value
        );
    }

    public static Activity? StartProcessActivity(
        BasicDeliverEventArgs delivery,
        string queue
    )
    {
        ArgumentNullException.ThrowIfNull(delivery);

        IDictionary<string, object?>? headers = delivery.BasicProperties.Headers;
        PropagationContext parent = Propagator.Extract(default, headers, ReadHeader);
        Baggage.Current = parent.Baggage;

        Activity? activity = Source.StartActivity($"{queue} process", ActivityKind.Consumer, parent.ActivityContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("messaging.destination.name", queue);
        activity?.SetTag("messaging.rabbitmq.destination.routing_key", delivery.RoutingKey);
        activity?.SetTag("messaging.message.id", delivery.BasicProperties.MessageId);
        activity?.SetTag("event.type", delivery.BasicProperties.Type);
        activity?.SetTag("correlation.id", EventBusHeaders.ReadString(headers, EventBusHeaders.CorrelationId));
        return activity;
    }

    private static IEnumerable<string> ReadHeader(
        IDictionary<string, object?>? headers,
        string key
    ) => headers is not null && headers.TryGetValue(key, out object? value) && value is byte[] bytes
        ? [Encoding.UTF8.GetString(bytes)]
        : [];
}
