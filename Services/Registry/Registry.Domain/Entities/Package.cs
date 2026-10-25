using Registry.Domain.Exceptions;

namespace Registry.Domain.Entities;

public sealed class Package : AuditableEntity
{
    private readonly List<PackageVersion> _versions = [];
    private readonly List<PackageTag> _packageTags = [];

    private Package()
    {
    }

    public Package(
        long publisherId,
        string name,
        string? description,
        string? projectUrl,
        decimal licensePrice
    )
    {
        if (publisherId <= 0)
        {
            throw new DomainException("A package needs an existing publisher.");
        }

        ThrowIfInvalidName(name);

        if (projectUrl is not null && !projectUrl.StartsWith("https://", StringComparison.Ordinal))
        {
            throw new DomainException("Project URL must start with https://.");
        }

        PublisherId = publisherId;
        Name = name;
        Description = description;
        ProjectUrl = projectUrl;
        IsListed = true;
        ChangeLicensePrice(licensePrice);
    }

    public long PackageId { get; private set; }

    public long PublisherId { get; private set; }

    public Publisher Publisher { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public string? ProjectUrl { get; private set; }

    public decimal LicensePrice { get; private set; }

    public long DownloadCount { get; private set; }

    public bool IsListed { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<PackageVersion> Versions => _versions;

    public IReadOnlyCollection<PackageTag> PackageTags => _packageTags;

    public void Rename(string name)
    {
        ThrowIfInvalidName(name);

        if (name == Name)
        {
            throw new DomainException($"Package is already named '{name}'.");
        }

        Name = name;
    }

    public void ChangeLicensePrice(decimal licensePrice)
    {
        if (licensePrice < 0)
        {
            throw new DomainException("Licence price cannot be negative.");
        }

        LicensePrice = licensePrice;
    }

    public void RegisterDownloads(long count)
    {
        if (count <= 0)
        {
            throw new DomainException("Download count must be positive.");
        }

        if (!IsListed)
        {
            throw new DomainException($"Package '{Name}' is unlisted and cannot be downloaded.");
        }

        DownloadCount += count;
    }

    public void Unlist()
    {
        if (!IsListed)
        {
            throw new DomainException($"Package '{Name}' is already unlisted.");
        }

        IsListed = false;
    }

    public void Relist()
    {
        if (IsListed)
        {
            throw new DomainException($"Package '{Name}' is already listed.");
        }

        IsListed = true;
    }

    public PackageVersion PublishVersion(PackageVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        if (_versions.Any(existing => existing.SemVer == version.SemVer))
        {
            throw new DomainException($"Package '{Name}' already has version {version.SemVer}.");
        }

        _versions.Add(version);
        return version;
    }

    public PackageTag AssignTag(
        long tagId,
        DateTime assignedAt
    )
    {
        if (_packageTags.Any(existing => existing.TagId == tagId))
        {
            throw new DomainException($"Package '{Name}' already has tag {tagId}.");
        }

        PackageTag packageTag = new(this, tagId, assignedAt);
        _packageTags.Add(packageTag);
        return packageTag;
    }

    private static void ThrowIfInvalidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || !name.All(IsNameCharacter))
        {
            throw new DomainException("Package name must have at least 2 characters: letters, digits, dot, underscore or hyphen.");
        }
    }

    private static bool IsNameCharacter(
        char character
    ) => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-';
}
