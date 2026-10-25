using Registry.Bll.Dtos;

namespace Registry.Bll.Services.Interfaces;

public interface IPackageService
{
    Task<PagedResult<PackageDto>> GetListAsync(
        PackageQuery query,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<PackageDto>> GetTopDownloadedAsync(
        int take,
        CancellationToken cancellationToken
    );

    Task<PackageDetailsDto> GetByIdAsync(
        long packageId,
        CancellationToken cancellationToken
    );

    Task<PackageDto> CreateAsync(
        CreatePackageDto input,
        CancellationToken cancellationToken
    );

    Task RenameAsync(
        long packageId,
        RenamePackageDto input,
        CancellationToken cancellationToken
    );

    Task ChangeLicensePriceAsync(
        long packageId,
        ChangeLicensePriceDto input,
        CancellationToken cancellationToken
    );

    Task RegisterDownloadsAsync(
        long packageId,
        RegisterDownloadsDto input,
        CancellationToken cancellationToken
    );

    Task UnlistAsync(
        long packageId,
        CancellationToken cancellationToken
    );

    Task RelistAsync(
        long packageId,
        CancellationToken cancellationToken
    );

    Task AssignTagAsync(
        long packageId,
        AssignTagDto input,
        CancellationToken cancellationToken
    );

    Task DeleteAsync(
        long packageId,
        CancellationToken cancellationToken
    );
}
