using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Registry.Dal.Auditing;
using Registry.Dal.Outbox;
using Registry.Domain.Entities;

namespace Registry.Dal;

public sealed class RegistryDbContext(
    DbContextOptions<RegistryDbContext> options,
    ICurrentUser currentUser
)
    : DbContext(options)
{
    public DbSet<Publisher> Publishers => Set<Publisher>();

    public DbSet<Package> Packages => Set<Package>();

    public DbSet<PackageVersion> PackageVersions => Set<PackageVersion>();

    public DbSet<PackageManifest> PackageManifests => Set<PackageManifest>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<PackageTag> PackageTags => Set<PackageTag>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        DateTime now = DateTime.UtcNow;
        string? userName = currentUser.UserName;

        foreach (EntityEntry<AuditableEntity> entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added && userName is not null)
            {
                entry.Entity.MarkCreated(userName);
            }
            else if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Unchanged;
                entry.Entity.MarkDeleted(now, userName);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.MarkUpdated(now, userName);
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RegistryDbContext).Assembly);
    }
}
