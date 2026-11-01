using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Platform.Data.Email;

public sealed class EmailDbContext(DbContextOptions<EmailDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "email";

    public DbSet<OutgoingEmail> OutgoingEmails => Set<OutgoingEmail>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<OutgoingEmail>(entity =>
        {
            entity.ToTable("outgoing_emails");
            entity.HasKey(email => email.Id);
            entity.Property(email => email.Id).UseIdentityAlwaysColumn();
            entity.HasIndex(email => email.NextAttemptAt).HasFilter("sent_at IS NULL AND gave_up_at IS NULL");
        });
    }
}
