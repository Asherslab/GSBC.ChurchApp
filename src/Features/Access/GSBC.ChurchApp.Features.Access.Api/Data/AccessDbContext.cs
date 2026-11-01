using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api.Data;

public sealed class AccessDbContext(DbContextOptions<AccessDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "access";
    public const string ChangesChannel = "gsbc_access_changes";

    public DbSet<RoleRecord> Roles => Set<RoleRecord>();
    public DbSet<RolePattern> RolePatterns => Set<RolePattern>();
    public DbSet<RoleInclude> RoleIncludes => Set<RoleInclude>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<CatalogEntry> Catalog => Set<CatalogEntry>();

    protected override string Schema => SchemaName;

    public async Task<Dictionary<Guid, RoleRecord>> LoadRolesWithIncludesAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken)
    {
        List<Guid> ids = roleIds.Distinct().ToList();
        List<RoleRecord> direct = await Roles.AsNoTracking().Include(role => role.Patterns).Include(role => role.Includes)
            .Where(role => ids.Contains(role.Id)).ToListAsync(cancellationToken);
        List<Guid> includedIds = direct.SelectMany(role => role.Includes.Select(include => include.IncludedRoleId)).Except(ids).ToList();
        List<RoleRecord> included = await Roles.AsNoTracking().Include(role => role.Patterns)
            .Where(role => includedIds.Contains(role.Id)).ToListAsync(cancellationToken);
        return direct.Concat(included).ToDictionary(role => role.Id);
    }

    public Task AnnounceChangeAsync(CancellationToken cancellationToken) =>
        Database.ExecuteSqlRawAsync($"NOTIFY {ChangesChannel}", cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RoleRecord>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);
            entity.HasIndex(role => role.SystemKey).IsUnique();
            entity.Ignore(role => role.IsSystem);
            entity.HasMany(role => role.Patterns).WithOne().HasForeignKey(pattern => pattern.RoleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(role => role.Includes).WithOne().HasForeignKey(include => include.RoleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RolePattern>(entity =>
        {
            entity.ToTable("role_patterns");
            entity.HasKey(pattern => new { pattern.RoleId, pattern.Pattern });
        });
        modelBuilder.Entity<RoleInclude>(entity =>
        {
            entity.ToTable("role_includes");
            entity.HasKey(include => new { include.RoleId, include.IncludedRoleId });
            entity.HasOne<RoleRecord>().WithMany().HasForeignKey(include => include.IncludedRoleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.ToTable("assignments");
            entity.HasKey(assignment => assignment.Id);
            entity.HasIndex(assignment => assignment.UserId);
            entity.Property(assignment => assignment.Source).HasConversion<string>();
            entity.HasOne(assignment => assignment.Role).WithMany().HasForeignKey(assignment => assignment.RoleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CatalogEntry>(entity =>
        {
            entity.ToTable("catalog");
            entity.HasKey(entry => entry.Node);
        });
    }
}
