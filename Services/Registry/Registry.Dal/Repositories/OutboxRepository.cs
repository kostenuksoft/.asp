using Microsoft.EntityFrameworkCore;
using Registry.Dal.Outbox;
using Registry.Dal.Repositories.Interfaces;

namespace Registry.Dal.Repositories;

public sealed class OutboxRepository(RegistryDbContext context) : IOutboxRepository
{
    public void Add(OutboxMessage message) => context.OutboxMessages.Add(message);

    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(
        DateTime occurredBefore,
        int take,
        CancellationToken cancellationToken = default
    ) => await context.OutboxMessages
        .Where(message => message.PublishedAt == null && message.OccurredAt < occurredBefore)
        .OrderBy(message => message.OutboxMessageId)
        .Take(take)
        .ToListAsync(cancellationToken);
}
