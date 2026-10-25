using MediatR;
using Microsoft.Extensions.Logging;
using Telemetry.Application.Common.Interfaces;

namespace Telemetry.Application.PackageCopies.Commands.SyncPackageName;

public sealed class SyncPackageNameCommandHandler(
    IPackageCopyRepository packageCopies,
    ILogger<SyncPackageNameCommandHandler> logger
)
    : IRequestHandler<SyncPackageNameCommand, SyncPackageNameResult>
{
    public async Task<SyncPackageNameResult> Handle(
        SyncPackageNameCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        if (await packageCopies.IsProcessedAsync(request.EventId, cancellationToken))
        {
            logger.LogInformation(
                "Duplicate {EventType} {EventId} for package {PackageId} skipped",
                request.EventType,
                request.EventId,
                request.PackageId
            );
            return new SyncPackageNameResult(true, 0, 0);
        }

        PackageCopyUpdate update = await packageCopies.RefreshNameAsync(
            request.PackageId,
            request.PublisherSlug,
            request.PackageName,
            request.PackageKey,
            request.OccurredAt,
            cancellationToken
        );
        bool recorded = await packageCopies.MarkProcessedAsync(request.EventId, request.EventType, cancellationToken);

        logger.LogInformation(
            "Package {PackageId} name copies set to '{PackageKey}': {DiscussionCount} discussion(s), {InsightCount} insight(s)",
            request.PackageId,
            request.PackageKey,
            update.Discussions,
            update.Insights
        );

        return new SyncPackageNameResult(!recorded, update.Discussions, update.Insights);
    }
}
