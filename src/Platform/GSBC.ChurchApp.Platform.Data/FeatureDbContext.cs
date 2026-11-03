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
        FeedChangeTable.Map(modelBuilder, OwnsFeedChanges);
    }
}
