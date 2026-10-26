namespace Telemetry.Application.Common.Interfaces;

public interface IPackageCopyRepository
{
    Task<bool> IsProcessedAsync(
        Guid eventId,
        CancellationToken cancellationToken
    );

    Task<PackageCopyUpdate> RefreshNameAsync(
        long packageId,
        string publisherSlug,
        string packageName,
        string packageKey,
        DateTime changedAt,
        CancellationToken cancellationToken
    );

    Task<bool> MarkProcessedAsync(
        Guid eventId,
        string eventType,
        CancellationToken cancellationToken
    );
}
