using Registry.Bll.Dtos;
using Registry.Bll.Services;
using Registry.Bll.Services.Interfaces;

namespace Registry.Bll.Caching;

public sealed class CachedTagService(
    TagService inner,
    TwoLevelCache cache
)
    : ITagService
{
    public Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken) => cache.GetOrCreateAsync(
        CacheKeys.Tags,
        CachePolicies.Tags,
        inner.GetAllAsync,
        cancellationToken
    );

    public Task<TagWithPackagesDto> GetWithPackagesAsync(
        long tagId,
        CancellationToken cancellationToken
    ) => inner.GetWithPackagesAsync(tagId, cancellationToken);

    public Task<IReadOnlyList<PackageDto>> GetRecentlyTaggedPackagesAsync(
        string tagName,
        DateTime since,
        CancellationToken cancellationToken
    ) => inner.GetRecentlyTaggedPackagesAsync(tagName, since, cancellationToken);
}
