using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Entities;

namespace Registry.Dal.Configurations;

public sealed class PackageManifestConfiguration : IEntityTypeConfiguration<PackageManifest>
{
    public void Configure(EntityTypeBuilder<PackageManifest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PackageManifest", AuditConfiguration.Schema, table =>
        {
            table.HasCheckConstraint("CK_PackageManifest_TargetFramework", "[TargetFramework] LIKE N'net[0-9]%'");
            table.HasCheckConstraint("CK_PackageManifest_EntryPoints_Json", "[EntryPoints] IS NULL OR ISJSON([EntryPoints]) = 1");
        });

        builder.HasKey(manifest => manifest.PackageVersionId).HasName("PK_PackageManifest");

        builder.Property(manifest => manifest.PackageVersionId).ValueGeneratedNever();
        builder.Property(manifest => manifest.LicenseExpression).IsRequired().HasMaxLength(100);
        builder.Property(manifest => manifest.TargetFramework).IsRequired().HasMaxLength(50);
        builder.ConfigureAudit();
    }
}
