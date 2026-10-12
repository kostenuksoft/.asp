namespace Cicd.Domain.Models;

public sealed class Artifact : AuditableEntity
{
    public long ArtifactId { get; set; }

    public long PackageId { get; set; }

    public string PackageKey { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string PublisherSlug { get; set; } = string.Empty;

    public string SemVer { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public string StorageUri { get; set; } = string.Empty;

    public DateTime? RegistrySyncedAt { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
