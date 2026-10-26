using Platform.EventBus;
using RabbitMQ.Client;

namespace Registry.Api.IntegrationEvents;

public sealed class OutboxDispatcher(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcher> logger
)
    : BackgroundService
{
    private static readonly TimeSpan s_interval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan s_gracePeriod = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using (IChannel channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken))
        {
            await EventBusTopology.DeclareExchangeAsync(channel, RegistryEvents.Exchange, stoppingToken);
        }

        logger.LogInformation("Outbox dispatcher started: every {Interval}, rows older than {GracePeriod}", s_interval, s_gracePeriod);

        using PeriodicTimer timer = new(s_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                RegistryIntegrationEventService events = scope.ServiceProvider.GetRequiredService<RegistryIntegrationEventService>();
                int published = await events.PublishUnpublishedAsync(DateTime.UtcNow - s_gracePeriod, stoppingToken);
                if (published > 0)
                {
                    logger.LogInformation("Outbox dispatcher picked up {Count} unpublished event(s)", published);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Outbox dispatch failed; retrying in {Interval}", s_interval);
            }
        }
    }
}
