using RabbitMQ.Client;

namespace Platform.EventBus;

public static class EventBusTopology
{
    public static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    public static string DeadLetterExchangeOf(string exchange) => $"{exchange}.dlx";

    public static async Task DeclareExchangeAsync(
        IChannel channel,
        string exchange,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(channel);

        await channel.ExchangeDeclareAsync(
            exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken
        );
        await channel.ExchangeDeclareAsync(
            DeadLetterExchangeOf(exchange),
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken
        );
    }

    public static async Task DeclareSubscriptionAsync(
        IChannel channel,
        EventSubscription subscription,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(channel);
        ArgumentNullException.ThrowIfNull(subscription);

        await DeclareExchangeAsync(channel, subscription.Exchange, cancellationToken);

        await channel.QueueDeclareAsync(
            subscription.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchangeOf(subscription.Exchange),
                ["x-dead-letter-routing-key"] = subscription.Queue,
            },
            cancellationToken: cancellationToken
        );
        foreach (string routingPattern in subscription.RoutingPatterns)
        {
            await channel.QueueBindAsync(subscription.Queue, subscription.Exchange, routingPattern, cancellationToken: cancellationToken);
        }

        await channel.QueueDeclareAsync(
            subscription.RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = (int)RetryDelay.TotalMilliseconds,
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = subscription.Queue,
            },
            cancellationToken: cancellationToken
        );

        await channel.QueueDeclareAsync(
            subscription.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken
        );
        await channel.QueueBindAsync(
            subscription.DeadLetterQueue,
            DeadLetterExchangeOf(subscription.Exchange),
            subscription.Queue,
            cancellationToken: cancellationToken
        );
    }
}
