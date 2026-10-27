using Microsoft.EntityFrameworkCore;
using Registry.Dal.Configurations;
using Registry.Domain.Entities;

namespace Registry.Dal.Seeding;

public static class DbInitializer
{
    private const long PlatformTeam = 1;
    private const long PaymentsTeam = 2;
    private const long ResearchTeam = 3;

    private const long Authentication = 1;
    private const long Storage = 2;
    private const long Payments = 3;
    private const long Infrastructure = 4;
    private const long Experimental = 5;

    public static async Task SeedAsync(
        RegistryDbContext context,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await context.Packages.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (Package package in CreatePackages())
        {
            context.Packages.Add(package);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static IEnumerable<Package> CreatePackages()
    {
        yield return Create(
            PaymentsTeam, "billing-sdk", "Client for the internal billing and invoicing API.", 49.00m, 6120, "net8.0", [Payments],
            (0, 9, 1, null, 524288, "e1d4c7b0a3f6e9d2c5b8a1f4e7d0c3b6a9f2e5d8c1b4a7f0e3d6c9b2a5f8e1d4", 14)
        );

        yield return Create(
            PlatformTeam, "devhub-auth", "Token issuance and claim validation for DevHub services.", 0m, 48210, "net8.0", [Authentication, Infrastructure],
            (1, 9, 0, null, 987136, "0a1b2c3d4e5f60718293a4b5c6d7e8f90a1b2c3d4e5f60718293a4b5c6d7e8f9", 180),
            (2, 0, 0, null, 1048576, "3b1f9c2a7d4e8f0b6c5a9e2d7f1b4c8a0e3d6f9b2c5a8e1d4f7b0c3a6e9d2f5b", 60),
            (2, 1, 0, null, 1130496, "9f2e5d8c1b4a7e0d3c6f9b2e5a8d1c4f7b0e3a6d9c2f5b8e1a4d7c0f3b6e9a2d", 7)
        );

        yield return Create(
            PlatformTeam, "devhub-storage", "Object storage abstraction over S3 and Azure Blob.", 0m, 31544, "net8.0", [Storage, Infrastructure],
            (1, 4, 2, null, 2097152, "c4a7e0d3b6f9c2e5a8d1b4f7e0a3d6c9b2e5f8a1d4c7b0e3f6a9d2c5b8e1f4a7", 21)
        );

        yield return Create(
            ResearchTeam, "graph-explorer", "Experimental dependency graph visualiser.", 0m, 87, "net9.0", [Experimental, Infrastructure],
            (0, 1, 0, "alpha", 262144, "f5a8d1c4b7e0a3f6d9c2b5e8a1f4d7c0b3e6a9f2d5c8b1e4a7f0d3c6b9e2a5f8", 3)
        );

        yield return Create(
            PlatformTeam, "devhub-logging", "Structured logging defaults for DevHub services.", 0m, 27903, "net8.0", [Infrastructure],
            (3, 2, 0, null, 412672, "1a2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f809", 40)
        );

        yield return Create(
            PlatformTeam, "devhub-config", "Typed configuration loader with secret references.", 0m, 19455, "net8.0", [Infrastructure],
            (1, 0, 3, null, 198656, "2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f8091a", 55)
        );

        yield return Create(
            PlatformTeam, "devhub-messaging", "Publisher and consumer helpers for the event bus.", 0m, 8840, "net9.0", [Infrastructure],
            (0, 8, 0, null, 356352, "3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f8091a2b", 12)
        );

        yield return Create(
            PaymentsTeam, "invoice-renderer", "Renders invoices to PDF from billing documents.", 129.00m, 2315, "net8.0", [Payments],
            (2, 3, 1, null, 3145728, "4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c", 33)
        );

        yield return Create(
            PaymentsTeam, "tax-rules", "Tax rate tables and rounding rules per jurisdiction.", 249.50m, 1178, "net8.0", [Payments],
            (5, 0, 0, null, 734208, "5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d", 9)
        );

        yield return Create(
            PaymentsTeam, "ledger-client", "Client for the double-entry ledger service.", 19.99m, 4022, "net8.0", [Payments, Infrastructure],
            (1, 2, 0, null, 289792, "6f708192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e", 26)
        );

        yield return Create(
            ResearchTeam, "model-zoo", "Catalogue of experimental scoring models.", 0m, 342, "net9.0", [Experimental],
            (0, 4, 0, "beta", 5242880, "708192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f", 5)
        );

        yield return Create(
            ResearchTeam, "trace-lab", "Trace sampling experiments for build telemetry.", 9.50m, 96, "net9.0", [Experimental, Storage],
            (0, 2, 1, null, 151552, "8192a3b4c5d6e7f8091a2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f70", 2)
        );
    }

    private static Package Create(
        long publisherId,
        string name,
        string description,
        decimal licensePrice,
        long downloads,
        string targetFramework,
        long[] tagIds,
        params (
            int Major,
            int Minor,
            int Patch,
            string? PreRelease,
            long SizeBytes,
            string Sha256,
            int DaysBeforeSeed
        )[] versions
    )
    {
        Package package = new(publisherId, name, description, null, licensePrice);
        package.RegisterDownloads(downloads);

        foreach ((
            int major,
            int minor,
            int patch,
            string? preRelease,
            long sizeBytes,
            string sha256,
            int daysBeforeSeed
        ) in versions)
        {
            DateTime publishedAt = AuditConfiguration.SeededAt.AddDays(-daysBeforeSeed);
            PackageVersion version = package.PublishVersion(
                new PackageVersion(major, minor, patch, preRelease, sizeBytes, sha256, publishedAt)
            );
            version.AttachManifest(new PackageManifest(
                "MIT",
                targetFramework,
                $"# {name} {version.SemVer}",
                $$"""{"lib":["lib/{{name}}.dll"]}"""
            ));
        }

        foreach (long tagId in tagIds)
        {
            package.AssignTag(tagId, AuditConfiguration.SeededAt.AddDays(-versions[0].DaysBeforeSeed));
        }

        return package;
    }
}
