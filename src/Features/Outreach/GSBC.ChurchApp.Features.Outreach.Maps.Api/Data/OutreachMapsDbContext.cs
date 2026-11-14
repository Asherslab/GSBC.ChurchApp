using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;

public sealed class OutreachMapsDbContext(DbContextOptions<OutreachMapsDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "outreach_maps";

    public DbSet<MapAreaRecord> Areas => Set<MapAreaRecord>();
    public DbSet<RegionRecord> Regions => Set<RegionRecord>();
    public DbSet<RegionFileRecord> Files => Set<RegionFileRecord>();

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
    }
}
