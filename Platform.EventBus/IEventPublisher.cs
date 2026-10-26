namespace Platform.EventBus;

public interface IEventPublisher
{
    Task PublishAsync(
        OutgoingMessage message,
        CancellationToken cancellationToken
    );
}
