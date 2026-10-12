namespace Registry.Bll.Dtos;

public sealed record PackageDto(
    long PackageId,
    long PublisherId,
    string? PublisherSlug,
    string Name,
    string? Description,
    string? ProjectUrl,
    decimal LicensePrice,
    long DownloadCount,
    bool IsListed
);
