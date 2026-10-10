namespace Cicd.Domain.Models;

public sealed class RunArtifact
{
    public string UsageKind { get; set; } = string.Empty;

    public long ArtifactId { get; set; }

    public long PackageId { get; set; }

    public string PackageKey { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string SemVer { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string StorageUri { get; set; } = string.Empty;
}
