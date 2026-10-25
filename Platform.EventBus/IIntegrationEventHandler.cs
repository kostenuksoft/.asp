namespace Platform.EventBus;

public interface IIntegrationEventHandler
{
    Task HandleAsync(
        IntegrationEvent integrationEvent,
        CancellationToken cancellationToken
    );
}

public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
    where TEvent : IntegrationEvent
{
    Task HandleAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken
    );

    Task IIntegrationEventHandler.HandleAsync(
        IntegrationEvent integrationEvent,
        CancellationToken cancellationToken
    ) => HandleAsync((TEvent)integrationEvent, cancellationToken);
}
