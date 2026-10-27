namespace Registry.Bll.Dtos;

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
