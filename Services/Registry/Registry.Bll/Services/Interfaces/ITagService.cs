using Registry.Bll.Dtos;

namespace Registry.Bll.Services.Interfaces;

public interface ITagService
{
    Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<TagWithPackagesDto> GetWithPackagesAsync(
        long tagId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<PackageDto>> GetRecentlyTaggedPackagesAsync(
        string tagName,
        DateTime since,
        CancellationToken cancellationToken
    );
}
