using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Platform.Data;

public abstract class FeatureDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<FeedChangeRecord> FeedChanges => Set<FeedChangeRecord>();

    protected abstract string Schema { get; }

    protected virtual bool OwnsFeedChanges => false;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<FeedChangeRecord>(entity =>
        {
            entity.ToTable(FeedChangeTable.Name, FeedChangeTable.Schema, table =>
            {
                if (!OwnsFeedChanges)
                    table.ExcludeFromMigrations();
            });
            entity.HasKey(change => change.Cursor);
            entity.Property(change => change.Cursor).UseIdentityAlwaysColumn();
            entity.Property(change => change.CommittedAt).HasDefaultValueSql("now()");
            entity.HasIndex(change => new { change.Feed, change.Cursor });
        });
    }
}
