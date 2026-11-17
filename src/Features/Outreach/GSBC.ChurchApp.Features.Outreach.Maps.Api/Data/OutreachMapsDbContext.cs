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
    public DbSet<MapSquareRecord> Squares => Set<MapSquareRecord>();
    public DbSet<PlaceRecord> Places => Set<PlaceRecord>();
    public DbSet<AddressRecord> Addresses => Set<AddressRecord>();
    public DbSet<OverviewRecord> Overviews => Set<OverviewRecord>();
    public DbSet<NationalAddressRecord> NationalAddresses => Set<NationalAddressRecord>();
    public DbSet<NationalLocalityRecord> NationalLocalities => Set<NationalLocalityRecord>();
    public DbSet<NationalReleaseRecord> NationalReleases => Set<NationalReleaseRecord>();
    public DbSet<PlaceMapRecord> PlaceMaps => Set<PlaceMapRecord>();

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
            entity.HasIndex(region => region.GridId);
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
            entity.HasKey(cell => new { cell.AreaId, cell.SquareId });
            entity.HasIndex(cell => new { cell.AreaId, cell.Row, cell.Column }).IsUnique();
            entity.HasIndex(cell => cell.SquareId);
        });
        modelBuilder.Entity<MapSquareRecord>(entity =>
        {
            entity.ToTable("squares");
            entity.HasKey(square => square.Id);
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
        modelBuilder.Entity<NationalAddressRecord>(entity =>
        {
            entity.ToTable("national_addresses");
            entity.HasKey(address => new { address.Id, address.Release });
            entity.HasIndex(address => address.Key).HasOperators("text_pattern_ops");
            entity.HasIndex(address => new { address.Number, address.Key }).HasOperators("text_ops", "text_pattern_ops");
            entity.HasIndex(address => address.BuildingKey).HasOperators("text_pattern_ops").HasFilter("building_key <> ''");
            entity.HasIndex(address => new { address.Latitude, address.Longitude });
            entity.HasIndex(address => address.Release);
        });
        modelBuilder.Entity<NationalLocalityRecord>(entity =>
        {
            entity.ToTable("national_localities");
            entity.HasKey(locality => new { locality.Id, locality.Release });
            entity.HasIndex(locality => locality.Key).HasOperators("text_pattern_ops");
        });
        modelBuilder.Entity<NationalReleaseRecord>(entity =>
        {
            entity.ToTable("national_releases");
            entity.HasKey(release => release.Id);
        });
        modelBuilder.Entity<PlaceMapRecord>(entity =>
        {
            entity.ToTable("place_maps");
            entity.HasKey(map => map.Id);
            entity.HasIndex(map => map.MapKey);
        });
    }
}
