namespace Cicd.Domain.Models;

public sealed class Package
{
    public long PackageId { get; set; }

    public string PublisherSlug { get; set; } = string.Empty;

    public string PackageName { get; set; } = string.Empty;

    public string PackageKey { get; set; } = string.Empty;

    public decimal LicensePrice { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? SourceChangedAt { get; set; }

    public DateTime SyncedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}
