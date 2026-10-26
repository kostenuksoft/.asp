using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Registry.Dal.Outbox;

namespace Registry.Dal.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("OutboxMessage", AuditConfiguration.Schema);

        builder.HasKey(message => message.OutboxMessageId).HasName("PK_OutboxMessage");
        builder.Property(message => message.OutboxMessageId).UseIdentityColumn();

        builder.Property(message => message.EventType).IsRequired().HasMaxLength(200);
        builder.Property(message => message.Exchange).IsRequired().HasMaxLength(200);
        builder.Property(message => message.RoutingKey).IsRequired().HasMaxLength(200);
        builder.Property(message => message.CorrelationId).HasMaxLength(100);
        builder.Property(message => message.OccurredAt).HasPrecision(3);
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.TraceParent).HasMaxLength(55).IsUnicode(false);
        builder.Property(message => message.PublishedAt).HasPrecision(3);
        builder.Property(message => message.LastError).HasMaxLength(1000);

        builder.HasIndex(message => message.EventId, "UQ_OutboxMessage_EventId").IsUnique();
        builder.HasIndex(message => message.OutboxMessageId, "IX_OutboxMessage_Unpublished")
            .HasFilter("[PublishedAt] IS NULL")
            .IncludeProperties(message => message.OccurredAt);
    }
}
