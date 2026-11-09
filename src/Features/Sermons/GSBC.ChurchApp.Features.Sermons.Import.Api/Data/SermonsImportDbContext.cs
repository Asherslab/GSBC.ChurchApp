using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Data;

public sealed class SermonsImportDbContext(DbContextOptions<SermonsImportDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "sermons_import";

    public DbSet<ImportRun> Runs => Set<ImportRun>();
    public DbSet<ImportedSermon> Sermons => Set<ImportedSermon>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ImportRun>(entity =>
        {
            entity.ToTable("runs");
            entity.HasKey(run => run.Id);
            entity.Property(run => run.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<ImportedSermon>(entity =>
        {
            entity.ToTable("sermons");
            entity.HasKey(sermon => sermon.WebsitePath);
            entity.HasIndex(sermon => sermon.SermonId);
        });
    }
}
