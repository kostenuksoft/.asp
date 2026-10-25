namespace Cicd.Dal.Repositories.Interfaces;

public interface IProcessedEventRepository
{
    Task<bool> TryAddAsync(
        Guid eventId,
        string eventType,
        CancellationToken cancellationToken = default
    );
}
