namespace Registry.Bll.Dtos;

public sealed record TaggedPackageDto(
    long PackageId,
    string PackageName,
    DateTime AssignedAt
);
