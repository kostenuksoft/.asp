using Cicd.Api.IntegrationEvents.Events;
using Cicd.Dal.UnitOfWork;

namespace Cicd.Api.IntegrationEvents.Handlers;

public sealed class PackageRenamedHandler(IUnitOfWork unitOfWork, ILogger<PackageRenamedHandler> logger)
    : PackageCopyHandler<PackageRenamedEvent>(unitOfWork, logger)
{
    protected override async Task OnAppliedAsync(
        PackageRenamedEvent integrationEvent,
        CancellationToken cancellationToken
    )
    {
        int pipelines = await UnitOfWork.Pipelines.UpdatePackageKeyAsync(
            integrationEvent.PackageId,
            integrationEvent.PackageKey,
            RegistryEventsActor,
            cancellationToken
        );

        Logger.LogInformation(
            "Package {PackageId} renamed '{PreviousPackageKey}' -> '{PackageKey}': local copy and {PipelineCount} pipeline(s) updated; artefact snapshots keep the old name",
            integrationEvent.PackageId,
            integrationEvent.PreviousPackageKey,
            integrationEvent.PackageKey,
            pipelines
        );
    }
}
