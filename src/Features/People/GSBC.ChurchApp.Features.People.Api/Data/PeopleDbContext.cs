using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.People.Api.Data;

public sealed class PeopleDbContext(DbContextOptions<PeopleDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "people";

    public DbSet<PersonRecord> People => Set<PersonRecord>();
    public DbSet<FamilyLink> Links => Set<FamilyLink>();
    public DbSet<LinkAskRecord> Asks => Set<LinkAskRecord>();
    public DbSet<SafeguardingEntry> Safeguarding => Set<SafeguardingEntry>();
    public DbSet<SmcPersonRecord> SmcPeople => Set<SmcPersonRecord>();
    public DbSet<SmcSyncState> SmcSync => Set<SmcSyncState>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<PersonRecord>(entity =>
        {
            entity.ToTable("people");
            entity.HasKey(person => person.UserId);
            entity.Property(person => person.UserId).ValueGeneratedNever();
        });
        modelBuilder.Entity<FamilyLink>(entity =>
        {
            entity.ToTable("links");
            entity.HasKey(link => new { link.ChildId, link.ParentId });
            entity.HasIndex(link => link.ParentId);
        });
        modelBuilder.Entity<LinkAskRecord>(entity =>
        {
            entity.ToTable("asks");
            entity.HasKey(ask => ask.Id);
            entity.Property(ask => ask.Id).ValueGeneratedNever();
            entity.HasIndex(ask => new { ask.ChildId, ask.ParentEmail }).IsUnique();
            entity.HasIndex(ask => ask.ParentEmail);
        });
        modelBuilder.Entity<SafeguardingEntry>(entity =>
        {
            entity.ToTable("safeguarding");
            entity.HasKey(entry => entry.UserId);
            entity.Property(entry => entry.UserId).ValueGeneratedNever();
            entity.HasIndex(entry => new { entry.Source, entry.ExternalId }).IsUnique().HasFilter("external_id IS NOT NULL");
        });
        modelBuilder.Entity<SmcPersonRecord>(entity =>
        {
            entity.ToTable("smc_people");
            entity.HasKey(person => person.SmcId);
        });
        modelBuilder.Entity<SmcSyncState>(entity =>
        {
            entity.ToTable("smc_sync");
            entity.HasKey(state => state.Id);
            entity.Property(state => state.Id).ValueGeneratedNever();
        });
    }
}
