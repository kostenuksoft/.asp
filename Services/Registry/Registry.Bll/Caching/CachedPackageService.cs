using Registry.Bll.Dtos;
using Registry.Bll.Services;
using Registry.Bll.Services.Interfaces;

namespace Registry.Bll.Caching;

public sealed class CachedPackageService(
    PackageService inner,
    TwoLevelCache cache
)
    : IPackageService
{
    public Task<PagedResult<PackageDto>> GetListAsync(
        PackageQuery query,
        CancellationToken cancellationToken
    ) => inner.GetListAsync(query, cancellationToken);

    public async Task<IReadOnlyList<PackageDto>> GetTopDownloadedAsync(
        int take,
        CancellationToken cancellationToken
    )
    {
        int count = Math.Clamp(take, 1, PackageService.MaxTopPackages);
        string? listVersion = await cache.GetVersionAsync(CacheKeys.PackageListVersion, cancellationToken);
        if (listVersion is null)
        {
            return await inner.GetTopDownloadedAsync(count, cancellationToken);
        }

        return await cache.GetOrCreateAsync(
            CacheKeys.TopPackages(count, listVersion),
            CachePolicies.PackageList,
            token => inner.GetTopDownloadedAsync(count, token),
            cancellationToken
        );
    }

    public Task<PackageDetailsDto> GetByIdAsync(
        long packageId,
        CancellationToken cancellationToken
    ) => cache.GetOrCreateAsync(
        CacheKeys.Package(packageId),
        CachePolicies.PackageCard,
        token => inner.GetByIdAsync(packageId, token),
        cancellationToken
    );

    public async Task<PackageDto> CreateAsync(
        CreatePackageDto input,
        CancellationToken cancellationToken
    )
    {
        PackageDto created = await inner.CreateAsync(input, cancellationToken);
        await cache.RenewVersionAsync(CacheKeys.PackageListVersion, "PackageCreated", CancellationToken.None);
        return created;
    }

    public async Task RenameAsync(
        long packageId,
        RenamePackageDto input,
        CancellationToken cancellationToken
    )
    {
        await inner.RenameAsync(packageId, input, cancellationToken);
        await InvalidateAsync(packageId, "PackageRenamed", listsChanged: true);
    }

    public async Task ChangeLicensePriceAsync(
        long packageId,
        ChangeLicensePriceDto input,
        CancellationToken cancellationToken
    )
    {
        await inner.ChangeLicensePriceAsync(packageId, input, cancellationToken);
        await InvalidateAsync(packageId, "LicensePriceChanged", listsChanged: true);
    }

    public async Task RegisterDownloadsAsync(
        long packageId,
        RegisterDownloadsDto input,
        CancellationToken cancellationToken
    )
    {
        await inner.RegisterDownloadsAsync(packageId, input, cancellationToken);
        await InvalidateAsync(packageId, "DownloadsRegistered", listsChanged: true);
    }

    public async Task UnlistAsync(
        long packageId,
        CancellationToken cancellationToken
    )
    {
        await inner.UnlistAsync(packageId, cancellationToken);
        await InvalidateAsync(packageId, "PackageUnlisted", listsChanged: true);
    }

    public async Task RelistAsync(
        long packageId,
        CancellationToken cancellationToken
    )
    {
        await inner.RelistAsync(packageId, cancellationToken);
        await InvalidateAsync(packageId, "PackageRelisted", listsChanged: true);
    }

    public async Task AssignTagAsync(
        long packageId,
        AssignTagDto input,
        CancellationToken cancellationToken
    )
    {
        await inner.AssignTagAsync(packageId, input, cancellationToken);
        await InvalidateAsync(packageId, "TagAssigned", listsChanged: false);
    }

    public async Task DeleteAsync(
        long packageId,
        CancellationToken cancellationToken
    )
    {
        await inner.DeleteAsync(packageId, cancellationToken);
        await InvalidateAsync(packageId, "PackageDeleted", listsChanged: true);
    }

    private async Task InvalidateAsync(
        long packageId,
        string reason,
        bool listsChanged
    )
    {
        await cache.RemoveAsync(CacheKeys.Package(packageId), reason, CancellationToken.None);
        if (listsChanged)
        {
            await cache.RenewVersionAsync(CacheKeys.PackageListVersion, reason, CancellationToken.None);
        }
    }
}
