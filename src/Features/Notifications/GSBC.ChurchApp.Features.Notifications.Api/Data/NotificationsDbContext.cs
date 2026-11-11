using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Notifications.Api.Data;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "notifications";

    public DbSet<NotificationRecord> Notifications => Set<NotificationRecord>();
    public DbSet<InboxEntry> Inbox => Set<InboxEntry>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Preference> Preferences => Set<Preference>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();
    public DbSet<AnnouncementRecord> Announcements => Set<AnnouncementRecord>();
    public DbSet<DigestRecord> Digests => Set<DigestRecord>();
    public DbSet<DigestItemRecord> DigestItems => Set<DigestItemRecord>();
    public DbSet<DigestScheduleRecord> DigestSchedules => Set<DigestScheduleRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NotificationRecord>(entity =>
        {
            entity.ToTable("notifications");
            entity.HasKey(notification => notification.Id);
            entity.HasIndex(notification => notification.RaisedAt);
        });
        modelBuilder.Entity<InboxEntry>(entity =>
        {
            entity.ToTable("inbox");
            entity.HasKey(entry => entry.Id);
            entity.HasIndex(entry => new { entry.UserId, entry.ReadAt });
            entity.HasOne(entry => entry.Notification).WithMany().HasForeignKey(entry => entry.NotificationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Device>(entity =>
        {
            entity.ToTable("devices");
            entity.HasKey(device => device.Id);
            entity.HasIndex(device => device.InstallId).IsUnique();
            entity.HasIndex(device => device.UserId);
        });
        modelBuilder.Entity<Preference>(entity =>
        {
            entity.ToTable("preferences");
            entity.HasKey(preference => preference.Id);
            entity.HasIndex(preference => new { preference.UserId, preference.InstallId, preference.Kind, preference.ScopeId }).IsUnique().AreNullsDistinct(false);
        });
        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.ToTable("deliveries");
            entity.HasKey(delivery => delivery.Id);
            entity.Property(delivery => delivery.Kind).HasConversion<string>();
            entity.HasIndex(delivery => new { delivery.SentAt, delivery.GaveUpAt, delivery.DueAt });
            entity.HasIndex(delivery => new { delivery.DeviceId, delivery.PacingGroup, delivery.CreatedAt });
            entity.HasOne<Device>().WithMany().HasForeignKey(delivery => delivery.DeviceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(delivery => delivery.Notification).WithMany().HasForeignKey(delivery => delivery.NotificationId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AnnouncementRecord>(entity =>
        {
            entity.ToTable("announcements");
            entity.HasKey(announcement => announcement.Id);
            entity.Property(announcement => announcement.State).HasConversion<string>();
            entity.HasIndex(announcement => new { announcement.State, announcement.SendAt });
        });
        modelBuilder.Entity<DigestRecord>(entity =>
        {
            entity.ToTable("digests");
            entity.HasKey(digest => digest.Id);
            entity.HasIndex(digest => new { digest.Kind, digest.GroupKey }).IsUnique();
            entity.HasIndex(digest => digest.DueAt);
        });
        modelBuilder.Entity<DigestItemRecord>(entity =>
        {
            entity.ToTable("digest_items");
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.DigestId, item.Id });
            entity.HasOne<DigestRecord>().WithMany().HasForeignKey(item => item.DigestId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DigestScheduleRecord>(entity =>
        {
            entity.ToTable("digest_schedules");
            entity.HasKey(schedule => schedule.Kind);
        });
    }
}
