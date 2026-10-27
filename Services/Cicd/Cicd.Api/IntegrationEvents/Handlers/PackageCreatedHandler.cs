using Cicd.Api.IntegrationEvents.Events;
using Cicd.Dal.UnitOfWork;

namespace Cicd.Api.IntegrationEvents.Handlers;

public sealed class PackageCreatedHandler(
    IUnitOfWork unitOfWork,
    ILogger<PackageCreatedHandler> logger
)
    : PackageCopyHandler<PackageCreatedEvent>(unitOfWork, logger)
{
    protected override Task OnAppliedAsync(
        PackageCreatedEvent integrationEvent,
        CancellationToken cancellationToken
    )
    {
        Logger.LogInformation(
            "Package {PackageId} '{PackageKey}' copied with licence price {LicensePrice}",
            integrationEvent.PackageId,
            integrationEvent.PackageKey,
            integrationEvent.LicensePrice
        );
        return Task.CompletedTask;
    }
}
