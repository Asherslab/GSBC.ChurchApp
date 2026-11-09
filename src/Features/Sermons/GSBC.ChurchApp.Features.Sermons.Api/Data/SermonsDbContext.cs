using System.Text.Json;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GSBC.ChurchApp.Features.Sermons.Api.Data;

public sealed class SermonsDbContext(DbContextOptions<SermonsDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "sermons";

    public DbSet<SermonRecord> Sermons => Set<SermonRecord>();
    public DbSet<SeriesRecord> Series => Set<SeriesRecord>();
    public DbSet<SpeakerRecord> Speakers => Set<SpeakerRecord>();
    public DbSet<ProgressRecord> Progress => Set<ProgressRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SermonRecord>(entity =>
        {
            entity.ToTable("sermons");
            entity.HasKey(sermon => sermon.Id);
            entity.HasIndex(sermon => sermon.PreachedAt);
            Media(entity.Property(sermon => sermon.Artwork));
            Media(entity.Property(sermon => sermon.AudioMedia));
        });
        modelBuilder.Entity<SeriesRecord>(entity =>
        {
            entity.ToTable("series");
            entity.HasKey(series => series.Id);
            Media(entity.Property(series => series.Artwork));
        });
        modelBuilder.Entity<SpeakerRecord>(entity =>
        {
            entity.ToTable("speakers");
            entity.HasKey(speaker => speaker.Id);
            Media(entity.Property(speaker => speaker.Photo));
        });
        modelBuilder.Entity<ProgressRecord>(entity =>
        {
            entity.ToTable("progress");
            entity.HasKey(progress => new { progress.UserId, progress.SermonId });
        });
    }

    private static void Media(PropertyBuilder<MediaRef?> property) =>
        property.HasColumnType("jsonb").HasConversion(
            media => media == null ? null : JsonSerializer.Serialize(media, JsonSerializerOptions.Web),
            json => json == null ? null : JsonSerializer.Deserialize<MediaRef>(json, JsonSerializerOptions.Web));
}
