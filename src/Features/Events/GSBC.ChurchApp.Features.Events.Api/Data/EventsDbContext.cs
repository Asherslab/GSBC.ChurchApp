using System.Text.Json;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GSBC.ChurchApp.Features.Events.Api.Data;

public sealed class EventsDbContext(DbContextOptions<EventsDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "events";

    public DbSet<CalendarRecord> Calendars => Set<CalendarRecord>();
    public DbSet<EventRecord> Events => Set<EventRecord>();
    public DbSet<RegularEventRecord> RegularEvents => Set<RegularEventRecord>();
    public DbSet<SpecialRecord> Specials => Set<SpecialRecord>();
    public DbSet<BreakRecord> Breaks => Set<BreakRecord>();
    public DbSet<RegularEventBreak> RegularEventBreaks => Set<RegularEventBreak>();
    public DbSet<ShowingRecord> Showings => Set<ShowingRecord>();
    public DbSet<PublishedRow> PublishedRows => Set<PublishedRow>();
    public DbSet<CalendarLinkRecord> CalendarLinks => Set<CalendarLinkRecord>();
    public DbSet<DetailsReminderRecord> DetailsReminders => Set<DetailsReminderRecord>();
    public DbSet<DigestRecord> Digests => Set<DigestRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CalendarRecord>(entity =>
        {
            entity.ToTable("calendars");
            entity.HasKey(calendar => calendar.Id);
            entity.HasIndex(calendar => calendar.Slug).IsUnique();
        });
        modelBuilder.Entity<EventRecord>(entity =>
        {
            entity.ToTable("events");
            entity.HasKey(record => record.Id);
            entity.HasIndex(record => new { record.CalendarId, record.StartDate });
            entity.HasIndex(record => new { record.CalendarId, record.SourceUid }).IsUnique().HasFilter("source_uid IS NOT NULL");
            Json(entity.Property(record => record.Fields));
        });
        modelBuilder.Entity<RegularEventRecord>(entity =>
        {
            entity.ToTable("regular_events");
            entity.HasKey(record => record.Id);
            entity.HasIndex(record => record.CalendarId);
            Json(entity.Property(record => record.Fields));
            Json(entity.Property(record => record.Pattern));
        });
        modelBuilder.Entity<SpecialRecord>(entity =>
        {
            entity.ToTable("specials");
            entity.HasKey(special => new { special.RegularEventId, special.Date });
            Json(entity.Property(special => special.Fields));
        });
        modelBuilder.Entity<BreakRecord>(entity =>
        {
            entity.ToTable("breaks");
            entity.HasKey(record => record.Id);
            entity.HasIndex(record => record.RegularEventId);
        });
        modelBuilder.Entity<RegularEventBreak>(entity =>
        {
            entity.ToTable("regular_event_breaks");
            entity.HasKey(link => new { link.RegularEventId, link.BreakId });
            entity.HasIndex(link => link.BreakId);
        });
        modelBuilder.Entity<ShowingRecord>(entity =>
        {
            entity.ToTable("showings");
            entity.HasKey(showing => showing.Id);
            entity.HasIndex(showing => new { showing.CalendarId, showing.EventId, showing.RegularEventId }).IsUnique().AreNullsDistinct(false);
        });
        modelBuilder.Entity<PublishedRow>(entity =>
        {
            entity.ToTable("published_rows");
            entity.HasKey(row => new { row.Feed, row.RecordId });
        });
        modelBuilder.Entity<CalendarLinkRecord>(entity =>
        {
            entity.ToTable("calendar_links");
            entity.HasKey(link => link.Secret);
            entity.HasIndex(link => link.UserId);
            entity.HasIndex(link => link.InstallId);
        });
        modelBuilder.Entity<DetailsReminderRecord>(entity =>
        {
            entity.ToTable("details_reminders");
            entity.HasKey(reminder => new { reminder.OccurrenceId, reminder.DaysBefore });
            entity.HasIndex(reminder => reminder.SettledAt);
        });
        modelBuilder.Entity<DigestRecord>(entity =>
        {
            entity.ToTable("digests");
            entity.HasKey(digest => digest.Id);
            entity.HasIndex(digest => digest.SentAt);
            entity.HasIndex(digest => digest.Start);
            Json(entity.Property(digest => digest.Entries));
            Json(entity.Property(digest => digest.Sent));
        });
    }

    private static void Json<T>(PropertyBuilder<T> property) where T : class, new() =>
        property.HasColumnType("jsonb").HasConversion(
            value => JsonSerializer.Serialize(value, JsonSerializerOptions.Web),
            json => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web) ?? new T(),
            new ValueComparer<T>(
                (first, second) => JsonSerializer.Serialize(first, JsonSerializerOptions.Web) == JsonSerializer.Serialize(second, JsonSerializerOptions.Web),
                value => JsonSerializer.Serialize(value, JsonSerializerOptions.Web).GetHashCode(StringComparison.Ordinal),
                value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonSerializerOptions.Web), JsonSerializerOptions.Web)!));
}
