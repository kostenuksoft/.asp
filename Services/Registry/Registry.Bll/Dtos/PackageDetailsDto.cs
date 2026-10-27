namespace Registry.Bll.Dtos;

public sealed record PackageDetailsDto(
    long PackageId,
    long PublisherId,
    string? PublisherSlug,
    string Name,
    string? Description,
    string? ProjectUrl,
    decimal LicensePrice,
    long DownloadCount,
    bool IsListed,
    IReadOnlyList<PackageVersionDto> Versions,
    IReadOnlyList<PackageTagDto> Tags
);
