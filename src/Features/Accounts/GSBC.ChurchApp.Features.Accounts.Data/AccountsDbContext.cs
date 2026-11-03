using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class AccountsDbContext(DbContextOptions<AccountsDbContext> options)
    : IdentityUserContext<AccountUser, Guid, IdentityUserClaim<Guid>, IdentityUserLogin<Guid>, IdentityUserToken<Guid>, IdentityUserPasskey<Guid>>(options)
{
    public const string Schema = "accounts";

    public DbSet<AccountSession> Sessions => Set<AccountSession>();
    public DbSet<OneTimeCode> Codes => Set<OneTimeCode>();
    public DbSet<AuditEntry> Audit => Set<AuditEntry>();
    public DbSet<AppInstall> Installs => Set<AppInstall>();
    public DbSet<InstallChallenge> InstallChallenges => Set<InstallChallenge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<AccountUser>().ToTable("users");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityUserPasskey<Guid>>().ToTable("user_passkeys");

        modelBuilder.Entity<AccountSession>(entity =>
        {
            entity.HasKey(session => session.Id);
            entity.HasIndex(session => session.UserId);
            entity.Property(session => session.Client).HasConversion<string>();
            entity.HasIndex(session => session.SecretHash).IsUnique();
        });

        modelBuilder.Entity<OneTimeCode>(entity =>
        {
            entity.HasKey(code => code.Hash);
            entity.HasIndex(code => new { code.UserId, code.Purpose });
            entity.Property(code => code.Purpose).HasConversion<string>();
        });

        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => new { entry.UserId, entry.At });
        });

        modelBuilder.Entity<AppInstall>(entity =>
        {
            entity.ToTable("installs");
            entity.HasKey(install => install.Id);
            entity.HasIndex(install => install.KeyId).IsUnique();
            entity.Property(install => install.Environment).HasConversion<string>();
        });

        modelBuilder.Entity<InstallChallenge>(entity =>
        {
            entity.ToTable("install_challenges");
            entity.HasKey(challenge => challenge.Challenge);
        });
    }
}
