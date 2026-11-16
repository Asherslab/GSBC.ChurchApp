using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;

public sealed class OutreachMapsDbContext(DbContextOptions<OutreachMapsDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "outreach_maps";

    public DbSet<MapAreaRecord> Areas => Set<MapAreaRecord>();
    public DbSet<RegionRecord> Regions => Set<RegionRecord>();
    public DbSet<RegionFileRecord> Files => Set<RegionFileRecord>();
    public DbSet<MapCellRecord> Cells => Set<MapCellRecord>();
    public DbSet<PlaceRecord> Places => Set<PlaceRecord>();
    public DbSet<AddressRecord> Addresses => Set<AddressRecord>();
    public DbSet<OverviewRecord> Overviews => Set<OverviewRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MapAreaRecord>(entity =>
        {
            entity.ToTable("areas");
            entity.HasKey(area => area.Id);
            entity.Property(area => area.Report).HasColumnType("jsonb");
        });
        modelBuilder.Entity<RegionRecord>(entity =>
        {
            entity.ToTable("regions");
            entity.HasKey(region => region.Id);
            entity.HasIndex(region => region.AreaId);
        });
        modelBuilder.Entity<RegionFileRecord>(entity =>
        {
            entity.ToTable("files");
            entity.HasKey(file => new { file.Key, file.RegionId, file.Version });
            entity.HasIndex(file => file.RegionId);
        });
        modelBuilder.Entity<MapCellRecord>(entity =>
        {
            entity.ToTable("cells");
            entity.HasKey(cell => cell.Id);
            entity.HasIndex(cell => new { cell.AreaId, cell.Row, cell.Column }).IsUnique();
        });
        modelBuilder.Entity<PlaceRecord>(entity =>
        {
            entity.ToTable("places");
            entity.HasKey(place => place.Id);
            entity.Property(place => place.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<AddressRecord>(entity =>
        {
            entity.ToTable("addresses");
            entity.HasKey(address => new { address.Id, address.RegionId });
            entity.Property(address => address.Id).ValueGeneratedNever();
            entity.HasIndex(address => address.RegionId);
            entity.HasIndex(address => address.Key).HasOperators("text_pattern_ops");
            entity.HasIndex(address => new { address.Number, address.Key }).HasOperators("text_ops", "text_pattern_ops");
        });
        modelBuilder.Entity<OverviewRecord>(entity =>
        {
            entity.ToTable("overviews");
            entity.HasKey(overview => overview.Id);
        });
    }
}
