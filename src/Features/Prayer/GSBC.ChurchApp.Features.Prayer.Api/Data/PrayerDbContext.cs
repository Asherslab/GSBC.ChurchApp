using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Prayer.Api.Data;

public sealed class PrayerDbContext(DbContextOptions<PrayerDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "prayer";

    public DbSet<RequestRecord> Requests => Set<RequestRecord>();
    public DbSet<NoteRecord> Notes => Set<NoteRecord>();
    public DbSet<StepRecord> Steps => Set<StepRecord>();
    public DbSet<PrayedRecord> Prayed => Set<PrayedRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<RequestRecord>(entity =>
        {
            entity.ToTable("requests");
            entity.HasKey(request => request.Id);
            entity.Property(request => request.Id).ValueGeneratedNever();
            entity.HasIndex(request => request.UserId);
            entity.HasIndex(request => new { request.InstallId, request.SentAt });
            entity.HasIndex(request => request.ChangedAt);
            entity.HasMany(request => request.Notes).WithOne().HasForeignKey(note => note.RequestId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(request => request.Steps).WithOne().HasForeignKey(step => step.RequestId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<NoteRecord>(entity =>
        {
            entity.ToTable("notes");
            entity.HasKey(note => note.Id);
            entity.Property(note => note.Id).ValueGeneratedNever();
        });
        modelBuilder.Entity<StepRecord>(entity =>
        {
            entity.ToTable("steps");
            entity.HasKey(step => step.Id);
        });
        modelBuilder.Entity<PrayedRecord>(entity =>
        {
            entity.ToTable("prayed");
            entity.HasKey(prayed => new { prayed.RequestId, prayed.InstallId });
            entity.HasIndex(prayed => new { prayed.InstallId, prayed.At });
            entity.HasIndex(prayed => prayed.UserId);
            entity.HasOne<RequestRecord>().WithMany().HasForeignKey(prayed => prayed.RequestId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
