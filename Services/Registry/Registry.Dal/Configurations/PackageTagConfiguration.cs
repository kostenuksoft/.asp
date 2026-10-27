using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Entities;

namespace Registry.Dal.Configurations;

public sealed class PackageTagConfiguration : IEntityTypeConfiguration<PackageTag>
{
    public void Configure(EntityTypeBuilder<PackageTag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PackageTag", AuditConfiguration.Schema);

        builder.HasKey(packageTag => new { packageTag.PackageId, packageTag.TagId }).HasName("PK_PackageTag");

        builder.Property(packageTag => packageTag.AssignedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(packageTag => packageTag.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(packageTag => packageTag.CreatedBy)
            .IsRequired()
            .HasMaxLength(128)
            .HasDefaultValueSql("SUSER_SNAME()");

        builder
            .HasIndex(packageTag => new { packageTag.TagId, packageTag.AssignedAt }, "IX_PackageTag_TagId_AssignedAt")
            .IsDescending(false, true)
            .IncludeProperties(packageTag => packageTag.PackageId);

        builder.HasOne(packageTag => packageTag.Package)
            .WithMany(package => package.PackageTags)
            .HasForeignKey(packageTag => packageTag.PackageId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PackageTag_Package");

        builder.HasOne(packageTag => packageTag.Tag)
            .WithMany(tag => tag.PackageTags)
            .HasForeignKey(packageTag => packageTag.TagId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PackageTag_Tag");

        builder.HasQueryFilter(packageTag => !packageTag.Package.IsDeleted && !packageTag.Tag.IsDeleted);
    }
}
