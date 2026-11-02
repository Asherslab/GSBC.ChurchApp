using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Media.Api.Data;

public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "media";

    public DbSet<MediaItem> Items => Set<MediaItem>();
    public DbSet<PendingUpload> PendingUploads => Set<PendingUpload>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.ToTable("items");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Key);
            entity.HasIndex(item => item.UploadedAt);
            entity.Ignore(item => item.IsPrivate);
        });
        modelBuilder.Entity<PendingUpload>(entity =>
        {
            entity.ToTable("pending_uploads");
            entity.HasKey(upload => upload.Id);
            entity.HasIndex(upload => upload.ExpiresAt);
            entity.Ignore(upload => upload.StagingKey);
        });
    }
}
