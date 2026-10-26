using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus;

public abstract class RabbitMqConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger logger
) : BackgroundService
{
    public const int MaxRetries = 3;
    public const ushort PrefetchCount = 10;

    private static readonly CreateChannelOptions s_confirmedChannel = new(
        publisherConfirmationsEnabled: true,
        publisherConfirmationTrackingEnabled: true
    );

    private readonly SemaphoreSlim _processing = new(1, 1);
    private IChannel? _channel;

    protected abstract EventSubscription Subscription { get; }

    protected abstract IReadOnlyDictionary<string, Type> EventTypes { get; }

    protected virtual bool IsPermanentFailure(Exception exception) => false;

    public override void Dispose()
    {
        _channel?.Dispose();
        _processing.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await connection.CreateChannelAsync(s_confirmedChannel, stoppingToken);
        await EventBusTopology.DeclareSubscriptionAsync(_channel, Subscription, stoppingToken);
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, stoppingToken);

        AsyncEventingBasicConsumer consumer = new(_channel);
        consumer.ReceivedAsync += OnMessageAsync;
        string consumerTag = await _channel.BasicConsumeAsync(Subscription.Queue, autoAck: false, consumer, stoppingToken);
        logger.LogInformation(
            "Consuming {Queue} bound to {Exchange} with {RoutingPatterns}, prefetch {PrefetchCount}",
            Subscription.Queue,
            Subscription.Exchange,
            Subscription.RoutingPatterns,
            PrefetchCount
        );

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Stopping consumer of {Queue}", Subscription.Queue);
        }

        await _channel.BasicCancelAsync(consumerTag, noWait: false, CancellationToken.None);
        await _processing.WaitAsync(CancellationToken.None);
        try
        {
            await _channel.CloseAsync(CancellationToken.None);
        }
        finally
        {
            _processing.Release();
        }
    }

    private async Task OnMessageAsync(
        object sender,
        BasicDeliverEventArgs delivery
    )
    {
        IChannel channel = _channel!;
        IDictionary<string, object?>? headers = delivery.BasicProperties.Headers;
        string? eventType = delivery.BasicProperties.Type;
        string? eventId = delivery.BasicProperties.MessageId;
        int attempt = EventBusHeaders.ReadInt(headers, EventBusHeaders.RetryCount, 0);

        using Activity? activity = TracePropagation.StartProcessActivity(delivery, Subscription.Queue);
        using IDisposable? scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["EventId"] = eventId,
            ["EventType"] = eventType,
            ["CorrelationId"] = EventBusHeaders.ReadString(headers, EventBusHeaders.CorrelationId),
            ["Queue"] = Subscription.Queue,
            ["RetryCount"] = attempt,
        });

        await _processing.WaitAsync(CancellationToken.None);
        try
        {
            logger.LogInformation("Received {EventType} {EventId} from {Queue}", eventType, eventId, Subscription.Queue);

            IntegrationEvent integrationEvent = Deserialize(delivery);
            await using (AsyncServiceScope serviceScope = scopeFactory.CreateAsyncScope())
            {
                IIntegrationEventHandler handler = (IIntegrationEventHandler)serviceScope.ServiceProvider.GetRequiredService(
                    typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEvent.GetType())
                );
                await handler.HandleAsync(integrationEvent, CancellationToken.None);
            }

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
            logger.LogInformation("Acknowledged {EventType} {EventId}", eventType, eventId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            if (exception is JsonException or UnprocessableMessageException || IsPermanentFailure(exception))
            {
                logger.LogError(
                    exception,
                    "Rejected {EventType} {EventId} from {Queue}: permanent failure, sending to {DeadLetterQueue}",
                    eventType,
                    eventId,
                    Subscription.Queue,
                    Subscription.DeadLetterQueue
                );
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
            }
            else
            {
                await RetryOrDeadLetterAsync(channel, delivery, attempt, exception);
            }
        }
        finally
        {
            _processing.Release();
        }
    }

    private IntegrationEvent Deserialize(BasicDeliverEventArgs delivery)
    {
        string? eventType = delivery.BasicProperties.Type;
        if (eventType is null || !EventTypes.TryGetValue(eventType, out Type? clrType))
        {
            throw new UnprocessableMessageException($"Event type '{eventType}' is not handled by {Subscription.Queue}.");
        }

        int schemaVersion = EventBusHeaders.ReadInt(
            delivery.BasicProperties.Headers,
            EventBusHeaders.SchemaVersion,
            IntegrationEvent.SchemaVersion
        );
        if (schemaVersion > IntegrationEvent.SchemaVersion)
        {
            throw new UnprocessableMessageException($"Schema version {schemaVersion} of {eventType} is newer than {IntegrationEvent.SchemaVersion}.");
        }

        return JsonSerializer.Deserialize(delivery.Body.Span, clrType, EventBusJson.Options) as IntegrationEvent
            ?? throw new UnprocessableMessageException($"Message {delivery.BasicProperties.MessageId} has an empty body.");
    }

    private async Task RetryOrDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        int attempt,
        Exception exception
    )
    {
        string? eventType = delivery.BasicProperties.Type;
        string? eventId = delivery.BasicProperties.MessageId;

        if (attempt >= MaxRetries)
        {
            logger.LogError(
                exception,
                "Failed {EventType} {EventId} after {Attempts} retries, sending to {DeadLetterQueue}",
                eventType,
                eventId,
                attempt,
                Subscription.DeadLetterQueue
            );
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        Dictionary<string, object?> headers = new(delivery.BasicProperties.Headers ?? new Dictionary<string, object?>());
        headers[EventBusHeaders.RetryCount] = attempt + 1;
        BasicProperties properties = new(delivery.BasicProperties);
        properties.Headers = headers;

        await channel.BasicPublishAsync(string.Empty, Subscription.RetryQueue, mandatory: true, properties, delivery.Body);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false);
        logger.LogWarning(
            exception,
            "Retry {Attempt}/{MaxRetries} of {EventType} {EventId} in {RetryDelay} through {RetryQueue}",
            attempt + 1,
            MaxRetries,
            eventType,
            eventId,
            EventBusTopology.RetryDelay,
            Subscription.RetryQueue
        );
    }
}
