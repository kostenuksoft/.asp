using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Entities;

namespace Registry.Dal.Configurations;

public sealed class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Publisher", AuditConfiguration.Schema, table =>
        {
            table.HasCheckConstraint(
                "CK_Publisher_Slug_Format",
                "LEN([Slug]) >= 2 AND [Slug] NOT LIKE N'%[^a-z0-9-]%'"
            );
            table.HasCheckConstraint("CK_Publisher_ContactEmail", "[ContactEmail] LIKE N'%_@_%._%'");
        });

        builder.HasKey(publisher => publisher.PublisherId).HasName("PK_Publisher");

        builder.Property(publisher => publisher.Slug).IsRequired().HasMaxLength(100);
        builder.Property(publisher => publisher.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(publisher => publisher.ContactEmail).IsRequired().HasMaxLength(256);
        builder.Property(publisher => publisher.IsVerified).HasDefaultValue(false);
        builder.Property(publisher => publisher.RowVersion).IsRowVersion();
        builder.ConfigureAudit();

        builder.HasIndex(publisher => publisher.Slug, "UQ_Publisher_Slug").IsUnique();
        builder.HasIndex(publisher => publisher.ContactEmail, "UQ_Publisher_ContactEmail").IsUnique();

        builder.HasData(
            new { PublisherId = 1L, Slug = "platform-team", DisplayName = "Platform Engineering", ContactEmail = "platform@devhub.local", IsVerified = true, CreatedAt = AuditConfiguration.SeededAt, CreatedBy = "seed", IsDeleted = false },
            new { PublisherId = 2L, Slug = "payments-team", DisplayName = "Payments Engineering", ContactEmail = "payments@devhub.local", IsVerified = true, CreatedAt = AuditConfiguration.SeededAt, CreatedBy = "seed", IsDeleted = false },
            new { PublisherId = 3L, Slug = "research-team", DisplayName = "Innovation Research", ContactEmail = "research@devhub.local", IsVerified = false, CreatedAt = AuditConfiguration.SeededAt, CreatedBy = "seed", IsDeleted = false }
        );
    }
}
