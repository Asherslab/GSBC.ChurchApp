using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Api.Data;

public sealed class OutreachDbContext(DbContextOptions<OutreachDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "outreach";

    public DbSet<SettingsRecord> Settings => Set<SettingsRecord>();
    public DbSet<CampaignRecord> Campaigns => Set<CampaignRecord>();
    public DbSet<CoverageRecord> Coverage => Set<CoverageRecord>();
    public DbSet<WalkRecord> Walks => Set<WalkRecord>();
    public DbSet<WalkPieceRecord> WalkPieces => Set<WalkPieceRecord>();
    public DbSet<SkipRecord> Skips => Set<SkipRecord>();
    public DbSet<DoorRecord> Doors => Set<DoorRecord>();
    public DbSet<FollowUpRecord> FollowUps => Set<FollowUpRecord>();
    public DbSet<ReportRecord> Reports => Set<ReportRecord>();
    public DbSet<ExclusionZoneRecord> ExclusionZones => Set<ExclusionZoneRecord>();
    public DbSet<ExclusionAddressRecord> ExclusionAddresses => Set<ExclusionAddressRecord>();
    public DbSet<DayRecord> Days => Set<DayRecord>();
    public DbSet<RouteRecord> Routes => Set<RouteRecord>();
    public DbSet<ClaimRecord> Claims => Set<ClaimRecord>();
    public DbSet<AreaRecord> Areas => Set<AreaRecord>();
    public DbSet<WalkerRecord> Walkers => Set<WalkerRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SettingsRecord>(entity =>
        {
            entity.ToTable("settings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<CampaignRecord>(entity =>
        {
            entity.ToTable("campaigns");
            entity.HasKey(campaign => campaign.Id);
            entity.Property(campaign => campaign.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<CoverageRecord>(entity =>
        {
            entity.ToTable("coverage");
            entity.HasKey(coverage => coverage.Id);
            entity.HasIndex(coverage => new { coverage.CampaignId, coverage.Street });
            entity.HasIndex(coverage => coverage.At);
            entity.HasIndex(coverage => coverage.WalkId);
        });
        modelBuilder.Entity<WalkRecord>(entity =>
        {
            entity.ToTable("walks");
            entity.HasKey(walk => walk.Id);
            entity.Property(walk => walk.Id).ValueGeneratedNever();
            entity.HasIndex(walk => new { walk.InstallId, walk.StartedAt });
            entity.HasIndex(walk => walk.UserId);
            entity.HasIndex(walk => walk.StartedAt);
        });
        modelBuilder.Entity<WalkPieceRecord>(entity =>
        {
            entity.ToTable("walk_pieces");
            entity.HasKey(piece => new { piece.WalkId, piece.Seq });
            entity.HasOne<WalkRecord>().WithMany().HasForeignKey(piece => piece.WalkId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<SkipRecord>(entity =>
        {
            entity.ToTable("skips");
            entity.HasKey(skip => skip.Id);
            entity.Property(skip => skip.Id).ValueGeneratedNever();
            entity.HasIndex(skip => new { skip.InstallId, skip.At });
            entity.HasIndex(skip => skip.UserId);
        });
        modelBuilder.Entity<DoorRecord>(entity =>
        {
            entity.ToTable("doors");
            entity.HasKey(door => door.Id);
            entity.Property(door => door.Id).ValueGeneratedNever();
            entity.HasIndex(door => door.At);
        });
        modelBuilder.Entity<FollowUpRecord>(entity =>
        {
            entity.ToTable("follow_ups");
            entity.HasKey(followUp => followUp.Id);
            entity.Property(followUp => followUp.Id).ValueGeneratedNever();
            entity.HasIndex(followUp => new { followUp.InstallId, followUp.At });
            entity.HasIndex(followUp => followUp.UserId);
        });
        modelBuilder.Entity<ReportRecord>(entity =>
        {
            entity.ToTable("reports");
            entity.HasKey(report => report.Id);
            entity.Property(report => report.Id).ValueGeneratedNever();
            entity.HasIndex(report => new { report.InstallId, report.At });
            entity.HasIndex(report => report.UserId);
        });
        modelBuilder.Entity<ExclusionZoneRecord>(entity =>
        {
            entity.ToTable("exclusion_zones");
            entity.HasKey(zone => zone.Id);
            entity.Property(zone => zone.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<ExclusionAddressRecord>(entity =>
        {
            entity.ToTable("exclusion_addresses");
            entity.HasKey(address => address.Id);
            entity.Property(address => address.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<DayRecord>(entity =>
        {
            entity.ToTable("days");
            entity.HasKey(day => day.Id);
            entity.Property(day => day.Id).ValueGeneratedNever();
            entity.HasIndex(day => day.StartsAt);
            entity.Property(day => day.Meeting).AsJson();
            entity.HasMany<RouteRecord>().WithOne().HasForeignKey(route => route.DayId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RouteRecord>(entity =>
        {
            entity.ToTable("routes");
            entity.HasKey(route => route.Id);
            entity.Property(route => route.Id).ValueGeneratedNever();
            entity.HasIndex(route => route.Code).IsUnique();
        });
        modelBuilder.Entity<ClaimRecord>(entity =>
        {
            entity.ToTable("claims");
            entity.HasKey(claim => claim.Id);
            entity.Property(claim => claim.Id).ValueGeneratedNever();
            entity.HasIndex(claim => claim.DayId);
            entity.HasIndex(claim => new { claim.InstallId, claim.At });
            entity.HasIndex(claim => claim.UserId);
            entity.HasOne<RouteRecord>().WithMany().HasForeignKey(claim => claim.RouteId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AreaRecord>(entity =>
        {
            entity.ToTable("areas");
            entity.HasKey(area => area.Id);
            entity.Property(area => area.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<WalkerRecord>(entity =>
        {
            entity.ToTable("walkers");
            entity.HasKey(walker => walker.UserId);
            entity.Property(walker => walker.UserId).ValueGeneratedNever();
        });
    }
}
