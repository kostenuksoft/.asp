using System.Globalization;
using Registry.Domain.Exceptions;

namespace Registry.Domain.Entities;

public sealed class PackageVersion : AuditableEntity
{
    private PackageVersion()
    {
    }

    public PackageVersion(
        int majorVersion,
        int minorVersion,
        int patchVersion,
        string? preReleaseTag,
        long sizeBytes,
        string sha256,
        DateTime publishedAt
    )
    {
        if (majorVersion < 0 || minorVersion < 0 || patchVersion < 0)
        {
            throw new DomainException("Version numbers cannot be negative.");
        }

        if (sizeBytes <= 0)
        {
            throw new DomainException("Package size must be positive.");
        }

        if (string.IsNullOrWhiteSpace(sha256) || sha256.Length != 64 || !sha256.All(char.IsAsciiHexDigitLower))
        {
            throw new DomainException("Sha256 must be 64 lowercase hexadecimal characters.");
        }

        MajorVersion = majorVersion;
        MinorVersion = minorVersion;
        PatchVersion = patchVersion;
        PreReleaseTag = string.IsNullOrWhiteSpace(preReleaseTag) ? null : preReleaseTag;
        SemVer = string.Create(CultureInfo.InvariantCulture, $"{majorVersion}.{minorVersion}.{patchVersion}")
            + (PreReleaseTag is null ? string.Empty : $"-{PreReleaseTag}");
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        PublishedAt = publishedAt;
    }

    public long PackageVersionId { get; private set; }

    public long PackageId { get; private set; }

    public Package Package { get; private set; } = null!;

    public string SemVer { get; private set; } = string.Empty;

    public int MajorVersion { get; private set; }

    public int MinorVersion { get; private set; }

    public int PatchVersion { get; private set; }

    public string? PreReleaseTag { get; private set; }

    public long SizeBytes { get; private set; }

    public string Sha256 { get; private set; } = string.Empty;

    public bool IsDeprecated { get; private set; }

    public DateTime PublishedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public PackageManifest? Manifest { get; private set; }

    public void AttachManifest(PackageManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (Manifest is not null)
        {
            throw new DomainException($"Version {SemVer} already has a manifest.");
        }

        Manifest = manifest;
    }

    public void Deprecate()
    {
        if (IsDeprecated)
        {
            throw new DomainException($"Version {SemVer} is already deprecated.");
        }

        IsDeprecated = true;
    }
}
