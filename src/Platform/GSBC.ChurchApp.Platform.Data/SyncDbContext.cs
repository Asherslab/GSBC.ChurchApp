using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Platform.Data;

public sealed class SyncDbContext(DbContextOptions<SyncDbContext> options) : FeatureDbContext(options)
{
    public DbSet<FeedHorizon> FeedHorizons => Set<FeedHorizon>();

    protected override string Schema => FeedChangeTable.Schema;

    protected override bool OwnsFeedChanges => true;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<FeedHorizon>(entity =>
        {
            entity.ToTable("feed_horizons");
            entity.HasKey(horizon => horizon.Feed);
        });
    }
}
