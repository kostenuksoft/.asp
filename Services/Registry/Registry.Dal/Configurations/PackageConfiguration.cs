using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Entities;

namespace Registry.Dal.Configurations;

public sealed class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Package", AuditConfiguration.Schema, table =>
        {
            table.HasCheckConstraint(
                "CK_Package_Name_Format",
                "LEN([Name]) >= 2 AND [Name] NOT LIKE N'%[^a-zA-Z0-9._-]%'"
            );
            table.HasCheckConstraint("CK_Package_DownloadCount_NonNegative", "[DownloadCount] >= 0");
            table.HasCheckConstraint(
                "CK_Package_ProjectUrl_Scheme",
                "[ProjectUrl] IS NULL OR [ProjectUrl] LIKE N'https://%'"
            );
            table.HasCheckConstraint("CK_Package_LicensePrice_NonNegative", "[LicensePrice] >= 0");
        });

        builder.HasKey(package => package.PackageId).HasName("PK_Package");

        builder.Property(package => package.Name).IsRequired().HasMaxLength(200);
        builder.Property(package => package.Description).HasMaxLength(2000);
        builder.Property(package => package.ProjectUrl).HasMaxLength(400);
        builder.Property(package => package.LicensePrice).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        builder.Property(package => package.DownloadCount).HasDefaultValue(0L);
        builder.Property(package => package.RowVersion).IsRowVersion();
        builder.ConfigureAudit();

        builder.HasIndex(package => new { package.PublisherId, package.Name }, "UQ_Package_Publisher_Name").IsUnique();

        builder.HasIndex(package => new { package.PublisherId, package.Name }, "IX_Package_PublisherId_Name")
            .IncludeProperties(package => new { package.Description, package.DownloadCount })
            .HasFilter("[IsDeleted] = 0 AND [IsListed] = 1");

        builder.HasIndex(package => package.DownloadCount, "IX_Package_DownloadCount")
            .IsDescending()
            .IncludeProperties(package => new { package.Name, package.PublisherId })
            .HasFilter("[IsDeleted] = 0 AND [IsListed] = 1");

        builder.HasOne(package => package.Publisher)
            .WithMany(publisher => publisher.Packages)
            .HasForeignKey(package => package.PublisherId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Package_Publisher");

        builder.HasMany(package => package.Versions)
            .WithOne(version => version.Package)
            .HasForeignKey(version => version.PackageId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PackageVersion_Package");
    }
}
