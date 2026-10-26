using Cicd.Api.IntegrationEvents.Events;
using Cicd.Domain.Exceptions;
using Platform.EventBus;
using RabbitMQ.Client;

namespace Cicd.Api.IntegrationEvents;

public sealed class RegistryEventsConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<RegistryEventsConsumer> logger
)
    : RabbitMqConsumer(connection, scopeFactory, logger)
{
    protected override EventSubscription Subscription { get; } = new("registry-events", "cicd.registry-sync", ["package.*"]);

    protected override IReadOnlyDictionary<string, Type> EventTypes { get; } = new Dictionary<string, Type>
    {
        [nameof(PackageCreatedEvent)] = typeof(PackageCreatedEvent),
        [nameof(PackageRenamedEvent)] = typeof(PackageRenamedEvent),
        [nameof(PackageLicensePriceChangedEvent)] = typeof(PackageLicensePriceChangedEvent),
        [nameof(PackageDeletedEvent)] = typeof(PackageDeletedEvent),
    };

    protected override bool IsPermanentFailure(Exception exception) => exception is ValidationException;
}
