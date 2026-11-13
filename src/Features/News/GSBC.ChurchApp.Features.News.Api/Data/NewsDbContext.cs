using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.News.Api.Data;

public sealed class NewsDbContext(DbContextOptions<NewsDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "news";

    public DbSet<PostRecord> Posts => Set<PostRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PostRecord>(entity =>
        {
            entity.ToTable("posts");
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Id).ValueGeneratedNever();
            entity.Property(post => post.Body).HasColumnType("jsonb");
            entity.Property(post => post.State).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(post => new { post.State, post.PublishAt });
            entity.HasIndex(post => new { post.Source, post.SourceId }).IsUnique();
            entity.HasIndex(post => post.AuthorId);
            entity.HasIndex(post => post.AskedById);
        });
    }
}
