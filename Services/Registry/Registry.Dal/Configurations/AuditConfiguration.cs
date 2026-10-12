using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Domain.Entities;

namespace Registry.Dal.Configurations;

internal static class AuditConfiguration
{
    public const string Schema = "registry";

    public static readonly DateTime SeededAt = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void ConfigureAudit<T>(this EntityTypeBuilder<T> builder)
        where T : AuditableEntity
    {
        builder.Property(entity => entity.CreatedAt).HasPrecision(3).HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.CreatedBy).IsRequired().HasMaxLength(128).HasDefaultValueSql("SUSER_SNAME()");
        builder.Property(entity => entity.UpdatedAt).HasPrecision(3);
        builder.Property(entity => entity.UpdatedBy).HasMaxLength(128);
        builder.Property(entity => entity.IsDeleted).HasDefaultValue(false);
        builder.HasQueryFilter(entity => !entity.IsDeleted);
    }
}
