namespace Registry.Bll.Dtos;

public sealed record TagDto(
    long TagId,
    string Name,
    string? Description
);

public sealed record TagWithPackagesDto(
    long TagId,
    string Name,
    string? Description,
    IReadOnlyList<TaggedPackageDto> Packages
);

public sealed record TaggedPackageDto(
    long PackageId,
    string PackageName,
    DateTime AssignedAt
);
