using Cicd.Api.IntegrationEvents.Events;
using Cicd.Dal.UnitOfWork;

namespace Cicd.Api.IntegrationEvents.Handlers;

public sealed class PackageDeletedHandler(IUnitOfWork unitOfWork, ILogger<PackageDeletedHandler> logger)
    : PackageCopyHandler<PackageDeletedEvent>(unitOfWork, logger)
{
    protected override bool MarksDeleted => true;

    protected override Task OnAppliedAsync(
        PackageDeletedEvent integrationEvent,
        CancellationToken cancellationToken
    )
    {
        Logger.LogInformation(
            "Package {PackageId} '{PackageKey}' marked deleted in the local copy",
            integrationEvent.PackageId,
            integrationEvent.PackageKey
        );
        return Task.CompletedTask;
    }
}
