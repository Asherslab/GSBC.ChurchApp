using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Rosters.Api.Data;

public sealed class RostersDbContext(DbContextOptions<RostersDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "rosters";

    public DbSet<PositionRecord> Positions => Set<PositionRecord>();
    public DbSet<TrainedRecord> Trained => Set<TrainedRecord>();
    public DbSet<SettingsRecord> Settings => Set<SettingsRecord>();
    public DbSet<DateRecord> Dates => Set<DateRecord>();
    public DbSet<AssignmentRecord> Assignments => Set<AssignmentRecord>();
    public DbSet<SwapRecord> Swaps => Set<SwapRecord>();
    public DbSet<AwayRecord> Away => Set<AwayRecord>();
    public DbSet<PublishedRecord> Published => Set<PublishedRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PositionRecord>(entity =>
        {
            entity.ToTable("positions");
            entity.HasKey(position => position.Id);
            entity.Property(position => position.Id).ValueGeneratedNever();
            entity.HasIndex(position => position.MinistryId);
        });
        modelBuilder.Entity<TrainedRecord>(entity =>
        {
            entity.ToTable("trained");
            entity.HasKey(trained => new { trained.PositionId, trained.PersonId });
            entity.HasIndex(trained => trained.PersonId);
        });
        modelBuilder.Entity<SettingsRecord>(entity =>
        {
            entity.ToTable("settings");
            entity.HasKey(settings => settings.MinistryId);
            entity.Property(settings => settings.MinistryId).ValueGeneratedNever();
        });
        modelBuilder.Entity<DateRecord>(entity =>
        {
            entity.ToTable("dates");
            entity.HasKey(date => date.Id);
            entity.Property(date => date.Id).ValueGeneratedNever();
            entity.HasIndex(date => new { date.MinistryId, date.EventKey }).IsUnique();
            entity.HasIndex(date => date.StartsAt);
        });
        modelBuilder.Entity<AssignmentRecord>(entity =>
        {
            entity.ToTable("assignments");
            entity.HasKey(assignment => assignment.Id);
            entity.Property(assignment => assignment.Id).ValueGeneratedNever();
            entity.HasIndex(assignment => assignment.DateId);
            entity.HasIndex(assignment => assignment.MinistryId);
            entity.HasIndex(assignment => assignment.PersonId);
        });
        modelBuilder.Entity<SwapRecord>(entity =>
        {
            entity.ToTable("swaps");
            entity.HasKey(swap => swap.Id);
            entity.Property(swap => swap.Id).ValueGeneratedNever();
            entity.HasIndex(swap => swap.AssignmentId);
            entity.HasIndex(swap => swap.MinistryId);
        });
        modelBuilder.Entity<AwayRecord>(entity =>
        {
            entity.ToTable("away");
            entity.HasKey(away => away.Id);
            entity.Property(away => away.Id).ValueGeneratedNever();
            entity.HasIndex(away => away.PersonId);
        });
        modelBuilder.Entity<PublishedRecord>(entity =>
        {
            entity.ToTable("published");
            entity.HasKey(published => new { published.Feed, published.RecordId });
            entity.HasIndex(published => published.MinistryId);
        });
    }
}
