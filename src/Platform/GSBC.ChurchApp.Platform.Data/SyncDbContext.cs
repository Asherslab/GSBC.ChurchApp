using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Platform.Data;

public sealed class SyncDbContext(DbContextOptions<SyncDbContext> options) : FeatureDbContext(options)
{
    public DbSet<FeedHorizon> FeedHorizons => Set<FeedHorizon>();
    public DbSet<AudienceMark> AudienceMarks => Set<AudienceMark>();
    public DbSet<SyncOrigin> Origin => Set<SyncOrigin>();

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
        modelBuilder.Entity<AudienceMark>(entity =>
        {
            entity.ToTable("audience_marks");
            entity.HasKey(mark => new { mark.SessionId, mark.Feed });
            entity.HasIndex(mark => mark.SeenAt);
        });
        modelBuilder.Entity<SyncOrigin>(entity =>
        {
            entity.ToTable("origin");
            entity.HasKey(origin => origin.Id);
            entity.Property(origin => origin.Id).HasDefaultValueSql("gen_random_uuid()");
        });
    }
}
