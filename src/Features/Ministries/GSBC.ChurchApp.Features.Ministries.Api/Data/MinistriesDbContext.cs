using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api.Data;

public sealed class MinistriesDbContext(DbContextOptions<MinistriesDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "ministries";

    public DbSet<MinistryRecord> Ministries => Set<MinistryRecord>();
    public DbSet<AskRecord> Asks => Set<AskRecord>();
    public DbSet<PostRecord> Posts => Set<PostRecord>();
    public DbSet<FileRecord> Files => Set<FileRecord>();
    public DbSet<RoundRecord> Rounds => Set<RoundRecord>();
    public DbSet<SitOutRecord> SitOuts => Set<SitOutRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MinistryRecord>(entity =>
        {
            entity.ToTable("ministries");
            entity.HasKey(ministry => ministry.Id);
            entity.Property(ministry => ministry.Id).ValueGeneratedNever();
            entity.HasIndex(ministry => ministry.ParentId);
        });
        modelBuilder.Entity<AskRecord>(entity =>
        {
            entity.ToTable("asks");
            entity.HasKey(ask => ask.Id);
            entity.Property(ask => ask.Id).ValueGeneratedNever();
            entity.HasIndex(ask => new { ask.MinistryId, ask.PersonId, ask.As }).IsUnique();
            entity.HasIndex(ask => ask.PersonId);
        });
        modelBuilder.Entity<PostRecord>(entity =>
        {
            entity.ToTable("posts");
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Id).ValueGeneratedNever();
            entity.Property(post => post.Content).HasColumnType("jsonb");
            entity.Property(post => post.ChurchNews).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(post => new { post.MinistryId, post.PostedAt });
        });
        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.ToTable("files");
            entity.HasKey(file => file.Id);
            entity.Property(file => file.Id).ValueGeneratedNever();
            entity.HasIndex(file => file.MinistryId);
        });
        modelBuilder.Entity<RoundRecord>(entity =>
        {
            entity.ToTable("rounds");
            entity.HasKey(round => round.Id);
            entity.Property(round => round.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<SitOutRecord>(entity =>
        {
            entity.ToTable("sit_outs");
            entity.HasKey(sitOut => new { sitOut.GroupId, sitOut.RoundId });
            entity.HasIndex(sitOut => sitOut.RoundId);
        });
    }
}
