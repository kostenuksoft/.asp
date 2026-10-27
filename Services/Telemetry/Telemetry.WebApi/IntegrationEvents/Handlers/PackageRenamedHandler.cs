using MediatR;
using Platform.EventBus;
using Telemetry.Application.PackageCopies.Commands.SyncPackageName;
using Telemetry.WebApi.IntegrationEvents.Events;

namespace Telemetry.WebApi.IntegrationEvents.Handlers;

public sealed class PackageRenamedHandler(
    ISender sender,
    ILogger<PackageRenamedHandler> logger
)
    : IIntegrationEventHandler<PackageRenamedEvent>
{
    public async Task HandleAsync(
        PackageRenamedEvent integrationEvent,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        SyncPackageNameResult result = await sender.Send(
            new SyncPackageNameCommand(
                integrationEvent.EventId,
                nameof(PackageRenamedEvent),
                integrationEvent.PackageId,
                integrationEvent.PublisherSlug,
                integrationEvent.PackageName,
                integrationEvent.PackageKey,
                integrationEvent.OccurredAt
            ),
            cancellationToken
        );

        if (!result.Duplicate)
        {
            logger.LogInformation(
                "Package {PackageId} renamed '{PreviousPackageKey}' -> '{PackageKey}': {DiscussionCount} discussion(s) and {InsightCount} insight(s) refreshed; crash reports keep the old name",
                integrationEvent.PackageId,
                integrationEvent.PreviousPackageKey,
                integrationEvent.PackageKey,
                result.DiscussionsUpdated,
                result.InsightsUpdated
            );
        }
    }
}
