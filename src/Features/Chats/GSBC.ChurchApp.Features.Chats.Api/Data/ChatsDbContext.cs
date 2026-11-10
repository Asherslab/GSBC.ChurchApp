using System.Text.Json;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GSBC.ChurchApp.Features.Chats.Api.Data;

public sealed class ChatsDbContext(DbContextOptions<ChatsDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "chats";

    public DbSet<ChatRecord> Chats => Set<ChatRecord>();
    public DbSet<MessageRecord> Messages => Set<MessageRecord>();
    public DbSet<MessageVersionRecord> Versions => Set<MessageVersionRecord>();
    public DbSet<ReactionRecord> Reactions => Set<ReactionRecord>();
    public DbSet<ParticipantRecord> Participants => Set<ParticipantRecord>();
    public DbSet<ConsentRecord> Consents => Set<ConsentRecord>();
    public DbSet<ReportRecord> Reports => Set<ReportRecord>();
    public DbSet<AuditRecord> Audit => Set<AuditRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ChatRecord>(entity =>
        {
            entity.ToTable("chats");
            entity.HasKey(chat => chat.Id);
            entity.Property(chat => chat.Id).ValueGeneratedNever();
            entity.HasIndex(chat => chat.MinistryId);
        });
        modelBuilder.Entity<MessageRecord>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).ValueGeneratedNever();
            entity.HasIndex(message => new { message.ChatId, message.Seq }).IsUnique();
            entity.HasIndex(message => new { message.SenderId, message.ClientId });
            Json(entity.Property(message => message.Attachments));
        });
        modelBuilder.Entity<MessageVersionRecord>(entity =>
        {
            entity.ToTable("message_versions");
            entity.HasKey(version => version.Id);
            entity.Property(version => version.Id).ValueGeneratedNever();
            entity.HasIndex(version => new { version.MessageId, version.Number }).IsUnique();
            Json(entity.Property(version => version.Attachments));
        });
        modelBuilder.Entity<ReactionRecord>(entity =>
        {
            entity.ToTable("reactions");
            entity.HasKey(reaction => new { reaction.MessageId, reaction.PersonId, reaction.Emoji });
        });
        modelBuilder.Entity<ParticipantRecord>(entity =>
        {
            entity.ToTable("participants");
            entity.HasKey(participant => new { participant.ChatId, participant.PersonId });
            entity.HasIndex(participant => participant.PersonId);
        });
        modelBuilder.Entity<ConsentRecord>(entity =>
        {
            entity.ToTable("consents");
            entity.HasKey(consent => new { consent.ChatId, consent.ChildId });
        });
        modelBuilder.Entity<ReportRecord>(entity =>
        {
            entity.ToTable("reports");
            entity.HasKey(report => report.Id);
            entity.Property(report => report.Id).ValueGeneratedNever();
            entity.HasIndex(report => report.ChatId);
        });
        modelBuilder.Entity<AuditRecord>(entity =>
        {
            entity.ToTable("audit");
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Id).ValueGeneratedNever();
            entity.HasIndex(entry => new { entry.ChatId, entry.At });
            entity.HasIndex(entry => new { entry.Action, entry.At });
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
