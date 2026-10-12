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

public sealed record PackageVersionDto(
    long PackageVersionId,
    string SemVer,
    long SizeBytes,
    string Sha256,
    bool IsDeprecated,
    DateTime PublishedAt,
    string? LicenseExpression,
    string? TargetFramework
);

public sealed record PackageTagDto(
    long TagId,
    string TagName,
    DateTime AssignedAt
);
