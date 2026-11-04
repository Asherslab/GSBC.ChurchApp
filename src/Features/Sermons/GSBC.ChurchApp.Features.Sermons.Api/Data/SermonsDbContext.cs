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
    public DbSet<SpeakerAlias> SpeakerAliases => Set<SpeakerAlias>();
    public DbSet<ProgressRecord> Progress => Set<ProgressRecord>();
    public DbSet<SourceState> SourceStates => Set<SourceState>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SermonRecord>(entity =>
        {
            entity.ToTable("sermons");
            entity.HasKey(sermon => sermon.Id);
            entity.HasIndex(sermon => sermon.ExternalId).IsUnique();
            entity.HasIndex(sermon => sermon.PreachedAt);
            Media(entity.Property(sermon => sermon.Artwork));
            Media(entity.Property(sermon => sermon.Thumbnail));
            entity.Ignore(sermon => sermon.EffectiveTitle);
            entity.Ignore(sermon => sermon.EffectiveSpeakerId);
            entity.Ignore(sermon => sermon.EffectiveSeriesId);
            entity.Ignore(sermon => sermon.EffectiveService);
            entity.Ignore(sermon => sermon.EffectiveYouTubeId);
        });
        modelBuilder.Entity<SeriesRecord>(entity =>
        {
            entity.ToTable("series");
            entity.HasKey(series => series.Id);
            entity.HasIndex(series => series.Slug).IsUnique();
            Media(entity.Property(series => series.Artwork));
            Media(entity.Property(series => series.Thumbnail));
            Media(entity.Property(series => series.ChosenArtwork));
        });
        modelBuilder.Entity<SpeakerRecord>(entity =>
        {
            entity.ToTable("speakers");
            entity.HasKey(speaker => speaker.Id);
            Media(entity.Property(speaker => speaker.Photo));
            Media(entity.Property(speaker => speaker.ChosenPhoto));
        });
        modelBuilder.Entity<SpeakerAlias>(entity =>
        {
            entity.ToTable("speaker_aliases");
            entity.HasKey(alias => alias.Alias);
            entity.HasIndex(alias => alias.SpeakerId);
        });
        modelBuilder.Entity<ProgressRecord>(entity =>
        {
            entity.ToTable("progress");
            entity.HasKey(progress => new { progress.UserId, progress.SermonId });
        });
        modelBuilder.Entity<SourceState>(entity =>
        {
            entity.ToTable("source_state");
            entity.HasKey(state => state.Id);
            entity.Property(state => state.Id).ValueGeneratedNever();
        });
    }

    private static void Media(PropertyBuilder<MediaRef?> property) =>
        property.HasColumnType("jsonb").HasConversion(
            media => media == null ? null : JsonSerializer.Serialize(media, JsonSerializerOptions.Web),
            json => json == null ? null : JsonSerializer.Deserialize<MediaRef>(json, JsonSerializerOptions.Web));
}
