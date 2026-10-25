using OpenTelemetry;
using Platform.EventBus;
using Platform.ServiceDefaults;
using Registry.Api.IntegrationEvents.Events;
using Registry.Bll.Services.Interfaces;
using Registry.Dal.Outbox;
using Registry.Dal.UnitOfWork;
using Registry.Domain.Entities;

namespace Registry.Api.IntegrationEvents;

public sealed class RegistryIntegrationEventService(
    IUnitOfWork unitOfWork,
    IEventPublisher publisher,
    IHttpContextAccessor httpContextAccessor,
    ILogger<RegistryIntegrationEventService> logger
)
    : IPackageEvents
{
    private const int DispatchBatchSize = 50;

    private readonly List<OutboxMessage> _staged = [];

    public void PackageCreated(Package package)
    {
        ArgumentNullException.ThrowIfNull(package);

        Stage(
            new PackageCreatedEvent(package.PackageId, package.Publisher.Slug, package.Name, KeyOf(package), package.LicensePrice),
            RegistryEvents.PackageCreated
        );
    }

    public void PackageRenamed(
        Package package,
        string previousName
    )
    {
        ArgumentNullException.ThrowIfNull(package);

        Stage(
            new PackageRenamedEvent(
                package.PackageId,
                package.Publisher.Slug,
                package.Name,
                KeyOf(package),
                package.LicensePrice,
                previousName,
                $"{package.Publisher.Slug}/{previousName}"
            ),
            RegistryEvents.PackageRenamed
        );
    }

    public void LicensePriceChanged(
        Package package,
        decimal previousPrice
    )
    {
        ArgumentNullException.ThrowIfNull(package);

        Stage(
            new PackageLicensePriceChangedEvent(
                package.PackageId,
                package.Publisher.Slug,
                package.Name,
                KeyOf(package),
                package.LicensePrice,
                previousPrice
            ),
            RegistryEvents.PackageLicensePriceChanged
        );
    }

    public void PackageDeleted(Package package)
    {
        ArgumentNullException.ThrowIfNull(package);

        Stage(
            new PackageDeletedEvent(package.PackageId, package.Publisher.Slug, package.Name, KeyOf(package), package.LicensePrice),
            RegistryEvents.PackageDeleted
        );
    }

    public async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        if (_staged.Count == 0)
        {
            return;
        }

        OutboxMessage[] staged = [.. _staged];
        _staged.Clear();
        await PublishAsync(staged, cancellationToken);
    }

    public async Task<int> PublishUnpublishedAsync(
        DateTime occurredBefore,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<OutboxMessage> unpublished;
        using (SuppressInstrumentationScope.Begin())
        {
            unpublished = await unitOfWork.Outbox.GetUnpublishedAsync(occurredBefore, DispatchBatchSize, cancellationToken);
        }

        if (unpublished.Count > 0)
        {
            await PublishAsync(unpublished, cancellationToken);
        }

        return unpublished.Count;
    }

    private static string KeyOf(Package package) => $"{package.Publisher.Slug}/{package.Name}";

    private void Stage(
        IntegrationEvent integrationEvent,
        string routingKey
    )
    {
        string? correlationId = httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        OutgoingMessage message = OutgoingMessage.Create(
            integrationEvent with { CorrelationId = string.IsNullOrEmpty(correlationId) ? null : correlationId },
            RegistryEvents.Exchange,
            routingKey
        );

        OutboxMessage outboxMessage = new(
            message.EventId,
            message.EventType,
            message.Exchange,
            message.RoutingKey,
            message.CorrelationId,
            message.OccurredAt,
            message.SchemaVersion,
            message.Payload,
            message.TraceParent
        );
        unitOfWork.Outbox.Add(outboxMessage);
        _staged.Add(outboxMessage);

        logger.LogDebug("Staged {EventType} {EventId} in the outbox", message.EventType, message.EventId);
    }

    private async Task PublishAsync(
        IReadOnlyList<OutboxMessage> outboxMessages,
        CancellationToken cancellationToken
    )
    {
        foreach (OutboxMessage outboxMessage in outboxMessages)
        {
            try
            {
                await publisher.PublishAsync(
                    new OutgoingMessage(
                        outboxMessage.EventId,
                        outboxMessage.EventType,
                        outboxMessage.Exchange,
                        outboxMessage.RoutingKey,
                        outboxMessage.CorrelationId,
                        outboxMessage.OccurredAt,
                        outboxMessage.SchemaVersion,
                        outboxMessage.Payload,
                        outboxMessage.TraceParent
                    ),
                    cancellationToken
                );
                outboxMessage.MarkPublished(DateTime.UtcNow);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                outboxMessage.RecordFailure(exception.Message);
                logger.LogWarning(
                    exception,
                    "Publishing {EventType} {EventId} failed on attempt {Attempts}; it stays in the outbox",
                    outboxMessage.EventType,
                    outboxMessage.EventId,
                    outboxMessage.Attempts
                );
                break;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
