using System.Text.Json;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GSBC.ChurchApp.Features.Configuration.Api.Data;

public sealed class ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "config";

    public DbSet<PlacementRecord> Placements => Set<PlacementRecord>();
    public DbSet<ChangeRecord> Changes => Set<ChangeRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlacementRecord>(entity =>
        {
            entity.ToTable("placements");
            entity.HasKey(placement => placement.Id);
            entity.HasIndex(placement => new { placement.Area, placement.ScopeId });
            entity.HasIndex(placement => new { placement.Area, placement.ScopeId, placement.Surface })
                .IsUnique()
                .AreNullsDistinct(false)
                .HasFilter("is_singleton");
            entity.Property(placement => placement.Revision).IsConcurrencyToken();
            entity.Property(placement => placement.Values)
                .HasColumnType("jsonb")
                .HasConversion(
                    values => JsonSerializer.Serialize(values, JsonSerializerOptions.Default),
                    json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonSerializerOptions.Default) ?? new Dictionary<string, string>(),
                    new ValueComparer<Dictionary<string, string>>(
                        (left, right) => left!.Count == right!.Count && !left.Except(right).Any(),
                        values => values.Aggregate(0, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
                        values => new Dictionary<string, string>(values)));
        });
        modelBuilder.Entity<ChangeRecord>(entity =>
        {
            entity.ToTable("changes");
            entity.HasKey(change => change.Id);
            entity.HasIndex(change => new { change.Area, change.ScopeId, change.At });
            entity.HasIndex(change => change.BatchId);
            entity.Property(change => change.Before).HasColumnType("jsonb");
            entity.Property(change => change.After).HasColumnType("jsonb");
        });
    }
}
