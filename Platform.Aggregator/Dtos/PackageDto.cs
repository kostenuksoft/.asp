namespace Platform.Aggregator.Dtos;

public sealed record PackageDto(
    long PackageId,
    string? PublisherSlug,
    string Name,
    string? Description,
    decimal LicensePrice,
    long DownloadCount,
    bool IsListed
);
