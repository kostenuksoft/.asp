using Cicd.Api.IntegrationEvents.Events;
using Cicd.Dal.UnitOfWork;

namespace Cicd.Api.IntegrationEvents.Handlers;

public sealed class PackageLicensePriceChangedHandler(IUnitOfWork unitOfWork, ILogger<PackageLicensePriceChangedHandler> logger)
    : PackageCopyHandler<PackageLicensePriceChangedEvent>(unitOfWork, logger)
{
    protected override Task OnAppliedAsync(
        PackageLicensePriceChangedEvent integrationEvent,
        CancellationToken cancellationToken
    )
    {
        Logger.LogInformation(
            "Package {PackageId} licence price {PreviousLicensePrice} -> {LicensePrice} in the local copy; artefact snapshots keep their price",
            integrationEvent.PackageId,
            integrationEvent.PreviousLicensePrice,
            integrationEvent.LicensePrice
        );
        return Task.CompletedTask;
    }
}
