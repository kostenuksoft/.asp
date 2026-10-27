using FluentValidation;
using Platform.EventBus;
using RabbitMQ.Client;
using Telemetry.Domain.Exceptions;
using Telemetry.WebApi.IntegrationEvents.Events;

namespace Telemetry.WebApi.IntegrationEvents;

public sealed class RegistryEventsConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<RegistryEventsConsumer> logger
)
    : RabbitMqConsumer(connection, scopeFactory, logger)
{
    protected override EventSubscription Subscription { get; } = new(
        "registry-events",
        "telemetry.registry-sync",
        ["package.renamed"]
    );

    protected override IReadOnlyDictionary<string, Type> EventTypes { get; } = new Dictionary<string, Type>
    {
        [nameof(PackageRenamedEvent)] = typeof(PackageRenamedEvent),
    };

    protected override bool IsPermanentFailure(Exception exception) =>
        exception is ValidationException or DomainException;
}
