using Registry.Dal.Outbox;

namespace Registry.Dal.Repositories.Interfaces;

public interface IOutboxRepository
{
    void Add(OutboxMessage message);

    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(
        DateTime occurredBefore,
        int take,
        CancellationToken cancellationToken = default
    );
}
