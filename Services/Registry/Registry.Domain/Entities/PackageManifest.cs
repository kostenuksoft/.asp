using Registry.Domain.Exceptions;

namespace Registry.Domain.Entities;

public sealed class PackageManifest : AuditableEntity
{
    private PackageManifest()
    {
    }

    public PackageManifest(
        string licenseExpression,
        string targetFramework,
        string? readmeMarkdown,
        string? entryPoints
    )
    {
        if (string.IsNullOrWhiteSpace(licenseExpression))
        {
            throw new DomainException("A manifest needs a licence expression.");
        }

        if (string.IsNullOrWhiteSpace(targetFramework)
            || targetFramework.Length < 4
            || !targetFramework.StartsWith("net", StringComparison.Ordinal)
            || !char.IsAsciiDigit(targetFramework[3]))
        {
            throw new DomainException("Target framework must look like net8.0.");
        }

        LicenseExpression = licenseExpression;
        TargetFramework = targetFramework;
        ReadmeMarkdown = readmeMarkdown;
        EntryPoints = entryPoints;
    }

    public long PackageVersionId { get; private set; }

    public string? ReadmeMarkdown { get; private set; }

    public string LicenseExpression { get; private set; } = string.Empty;

    public string TargetFramework { get; private set; } = string.Empty;

    public string? EntryPoints { get; private set; }
}
