namespace Cicd.Bll.Dtos;

public sealed record RunArtifactDto(
    string UsageKind,
    long ArtifactId,
    long PackageId,
    string PackageKey,
    string PackageName,
    string SemVer,
    long SizeBytes,
    string StorageUri
);
