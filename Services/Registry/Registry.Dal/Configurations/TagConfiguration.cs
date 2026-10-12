using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Entities;

namespace Registry.Dal.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Tag", AuditConfiguration.Schema, table =>
        {
            table.HasCheckConstraint("CK_Tag_Name_Format", "LEN([Name]) >= 2 AND [Name] NOT LIKE N'%[^a-z0-9-]%'");
        });

        builder.HasKey(tag => tag.TagId).HasName("PK_Tag");

        builder.Property(tag => tag.Name).IsRequired().HasMaxLength(50);
        builder.Property(tag => tag.Description).HasMaxLength(400);
        builder.ConfigureAudit();

        builder.HasIndex(tag => tag.Name, "UQ_Tag_Name").IsUnique();

        builder.HasData(
            new { TagId = 1L, Name = "authentication", Description = "Identity, tokens and access control", CreatedAt = AuditConfiguration.SeededAt, CreatedBy = "seed", IsDeleted = false },
            new { TagId = 2L, Name = "storage", Description = "Blob, file and object storage", CreatedAt = AuditConfiguration.SeededAt, CreatedBy = "seed", IsDeleted = false },
            new { TagId = 3L, Name = "payments", Description = "Billing, invoicing and settlement", CreatedAt = AuditConfiguration.SeededAt, CreatedBy = "seed", IsDeleted = false },
            new { TagId = 4L, Name = "infrastructure", Description = "Cross-cutting platform concerns", CreatedAt = AuditConfiguration.SeededAt, CreatedBy = "seed", IsDeleted = false },
            new { TagId = 5L, Name = "experimental", Description = "Not recommended for production use", CreatedAt = AuditConfiguration.SeededAt, CreatedBy = "seed", IsDeleted = false }
        );
    }
}
