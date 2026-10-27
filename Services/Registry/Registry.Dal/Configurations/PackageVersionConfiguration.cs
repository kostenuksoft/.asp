using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Entities;

namespace Registry.Dal.Configurations;

public sealed class PackageVersionConfiguration : IEntityTypeConfiguration<PackageVersion>
{
    public void Configure(EntityTypeBuilder<PackageVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PackageVersion", AuditConfiguration.Schema, table =>
        {
            table.HasCheckConstraint("CK_PackageVersion_SizeBytes_Positive", "[SizeBytes] > 0");
            table.HasCheckConstraint(
                "CK_PackageVersion_Numbers_NonNegative",
                "[MajorVersion] >= 0 AND [MinorVersion] >= 0 AND [PatchVersion] >= 0"
            );
            table.HasCheckConstraint(
                "CK_PackageVersion_SemVer_MatchesParts",
                "[SemVer] = CONCAT([MajorVersion], N'.', [MinorVersion], N'.', [PatchVersion], CASE WHEN [PreReleaseTag] IS NULL THEN N'' ELSE CONCAT(N'-', [PreReleaseTag]) END)"
            );
            table.HasCheckConstraint(
                "CK_PackageVersion_Sha256_Hex",
                "LEN([Sha256]) = 64 AND [Sha256] NOT LIKE '%[^0-9a-f]%'"
            );
        });

        builder.HasKey(version => version.PackageVersionId).HasName("PK_PackageVersion");

        builder.Property(version => version.SemVer).IsRequired().HasMaxLength(50);
        builder.Property(version => version.PreReleaseTag).HasMaxLength(50);
        builder.Property(version => version.Sha256).IsRequired().HasColumnType("char(64)");
        builder.Property(version => version.IsDeprecated).HasDefaultValue(false);
        builder.Property(version => version.PublishedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(version => version.RowVersion).IsRowVersion();
        builder.ConfigureAudit();

        builder.HasIndex(version => new { version.PackageId, version.SemVer }, "UQ_PackageVersion_Package_SemVer")
            .IsUnique();
        builder.HasIndex(version => version.Sha256, "UQ_PackageVersion_Sha256").IsUnique();

        builder
            .HasIndex(
                version => new { version.PackageId, version.PublishedAt },
                "IX_PackageVersion_Package_PublishedAt"
            )
            .IsDescending(false, true)
            .IncludeProperties(version => new { version.SemVer, version.SizeBytes, version.IsDeprecated })
            .HasFilter("[IsDeleted] = 0");

        builder
            .HasIndex(
                version => new { version.PackageId, version.MajorVersion, version.MinorVersion, version.PatchVersion },
                "IX_PackageVersion_Ordering"
            )
            .IsDescending(false, true, true, true)
            .IncludeProperties(version => new { version.SemVer, version.PublishedAt, version.SizeBytes })
            .HasFilter("[IsDeleted] = 0 AND [IsDeprecated] = 0");

        builder.HasOne(version => version.Manifest)
            .WithOne()
            .HasForeignKey<PackageManifest>(manifest => manifest.PackageVersionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PackageManifest_PackageVersion");
    }
}
