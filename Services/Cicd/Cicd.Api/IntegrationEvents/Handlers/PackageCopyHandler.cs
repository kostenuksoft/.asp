using Cicd.Api.IntegrationEvents.Events;
using Cicd.Dal.UnitOfWork;
using Cicd.Domain.Models;
using Platform.EventBus;

namespace Cicd.Api.IntegrationEvents.Handlers;

public abstract class PackageCopyHandler<TEvent>(IUnitOfWork unitOfWork, ILogger logger) : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent, IPackageSnapshot
{
    protected const string RegistryEventsActor = "registry-events";

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;

    protected ILogger Logger { get; } = logger;

    protected virtual bool MarksDeleted => false;

    public async Task HandleAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        string eventType = typeof(TEvent).Name;
        bool committed = false;
        await UnitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (!await UnitOfWork.ProcessedEvents.TryAddAsync(integrationEvent.EventId, eventType, cancellationToken))
            {
                Logger.LogInformation(
                    "Duplicate {EventType} {EventId} for package {PackageId} skipped",
                    eventType,
                    integrationEvent.EventId,
                    integrationEvent.PackageId
                );
                return;
            }

            bool applied = await UnitOfWork.Packages.UpsertAsync(CopyOf(integrationEvent), cancellationToken);
            if (applied)
            {
                await OnAppliedAsync(integrationEvent, cancellationToken);
            }
            else
            {
                Logger.LogInformation(
                    "Stale {EventType} {EventId} for package {PackageId} ignored: the local copy holds a change newer than {OccurredAt}",
                    eventType,
                    integrationEvent.EventId,
                    integrationEvent.PackageId,
                    integrationEvent.OccurredAt
                );
            }

            await UnitOfWork.CommitAsync(cancellationToken);
            committed = true;
        }
        finally
        {
            if (!committed)
            {
                await UnitOfWork.RollbackAsync(CancellationToken.None);
            }
        }
    }

    protected abstract Task OnAppliedAsync(
        TEvent integrationEvent,
        CancellationToken cancellationToken
    );

    private Package CopyOf(TEvent integrationEvent) => new()
    {
        PackageId = integrationEvent.PackageId,
        PublisherSlug = integrationEvent.PublisherSlug,
        PackageName = integrationEvent.PackageName,
        PackageKey = integrationEvent.PackageKey,
        LicensePrice = integrationEvent.LicensePrice,
        IsDeleted = MarksDeleted,
        SourceChangedAt = integrationEvent.OccurredAt,
        CreatedBy = RegistryEventsActor,
    };
}
