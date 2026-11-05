using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Storage;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Media.Api.Data;

public sealed class MediaDbContext(DbContextOptions<MediaDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "media";

    public DbSet<MediaItem> Items => Set<MediaItem>();
    public DbSet<PendingUpload> PendingUploads => Set<PendingUpload>();
    public DbSet<MediaUse> Uses => Set<MediaUse>();
    public DbSet<MediaFolder> Folders => Set<MediaFolder>();
    public DbSet<MediaRendition> Renditions => Set<MediaRendition>();
    public DbSet<MediaAlias> Aliases => Set<MediaAlias>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.ToTable("items");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Key);
            entity.Property(item => item.Sha256).HasMaxLength(ContentAddress.Sha256HexLength).IsFixedLength();
            entity.HasIndex(item => item.Sha256);
            entity.HasIndex(item => item.UploadedAt);
            entity.HasIndex(item => item.FolderId);
            entity.HasOne<MediaFolder>().WithMany().HasForeignKey(item => item.FolderId).OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(item => item.IsPrivate);
            entity.Ignore(item => item.KeepsOriginal);
            entity.HasIndex(item => item.Sizing).HasFilter("sizing = 1");
        });
        modelBuilder.Entity<MediaRendition>(entity =>
        {
            entity.ToTable("renditions");
            entity.HasKey(rendition => new { rendition.MediaId, rendition.Size });
            entity.HasIndex(rendition => rendition.Key);
            entity.Property(rendition => rendition.Sha256).HasMaxLength(ContentAddress.Sha256HexLength).IsFixedLength();
            entity.HasOne<MediaItem>().WithMany().HasForeignKey(rendition => rendition.MediaId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MediaAlias>(entity =>
        {
            entity.ToTable("aliases");
            entity.HasKey(alias => alias.Id);
            entity.HasIndex(alias => alias.MediaId);
            entity.HasOne<MediaItem>().WithMany().HasForeignKey(alias => alias.MediaId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<MediaFolder>(entity =>
        {
            entity.ToTable("folders");
            entity.HasKey(folder => folder.Id);
            entity.Property(folder => folder.Name).HasMaxLength(MediaFolders.MaximumNameLength);
            entity.HasIndex(folder => folder.ParentId);
            entity.HasIndex(folder => new { folder.SystemArea, folder.SystemScope }).IsUnique().AreNullsDistinct(false).HasFilter("system_area IS NOT NULL");
            entity.HasOne<MediaFolder>().WithMany().HasForeignKey(folder => folder.ParentId).OnDelete(DeleteBehavior.Restrict);
            entity.Ignore(folder => folder.IsSystem);
        });
        modelBuilder.Entity<PendingUpload>(entity =>
        {
            entity.ToTable("pending_uploads");
            entity.HasKey(upload => upload.Id);
            entity.HasIndex(upload => upload.ExpiresAt);
            entity.Ignore(upload => upload.StagingKey);
        });
        modelBuilder.Entity<MediaUse>(entity =>
        {
            entity.ToTable("uses");
            entity.HasKey(use => new { use.MediaId, use.Owner });
            entity.HasIndex(use => use.Owner);
            entity.HasOne<MediaItem>().WithMany().HasForeignKey(use => use.MediaId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
