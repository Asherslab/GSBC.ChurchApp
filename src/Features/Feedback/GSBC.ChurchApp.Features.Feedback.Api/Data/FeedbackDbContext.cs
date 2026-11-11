using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Feedback.Api.Data;

public sealed class FeedbackDbContext(DbContextOptions<FeedbackDbContext> options) : FeatureDbContext(options)
{
    public const string SchemaName = "feedback";

    public DbSet<TicketRecord> Tickets => Set<TicketRecord>();
    public DbSet<AttachmentRecord> Attachments => Set<AttachmentRecord>();
    public DbSet<NoteRecord> Notes => Set<NoteRecord>();
    public DbSet<StepRecord> Steps => Set<StepRecord>();
    public DbSet<BlockRecord> Blocks => Set<BlockRecord>();
    public DbSet<UploadAskRecord> UploadAsks => Set<UploadAskRecord>();
    public DbSet<HandledRequestRecord> HandledRequests => Set<HandledRequestRecord>();

    protected override string Schema => SchemaName;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TicketRecord>(entity =>
        {
            entity.ToTable("tickets");
            entity.HasKey(ticket => ticket.Id);
            entity.Property(ticket => ticket.Id).ValueGeneratedNever();
            entity.Property(ticket => ticket.Number).UseIdentityAlwaysColumn();
            entity.HasIndex(ticket => ticket.Number).IsUnique();
            entity.HasIndex(ticket => new { ticket.InstallId, ticket.SentAt });
            entity.HasIndex(ticket => new { ticket.UserId, ticket.SentAt });
            entity.HasIndex(ticket => ticket.ClosedAt);
            entity.HasMany(ticket => ticket.Attachments).WithOne().HasForeignKey(attachment => attachment.TicketId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(ticket => ticket.Notes).WithOne().HasForeignKey(note => note.TicketId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(ticket => ticket.Steps).WithOne().HasForeignKey(step => step.TicketId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<AttachmentRecord>(entity =>
        {
            entity.ToTable("attachments");
            entity.HasKey(attachment => new { attachment.TicketId, attachment.Position });
        });
        modelBuilder.Entity<NoteRecord>(entity =>
        {
            entity.ToTable("notes");
            entity.HasKey(note => note.Id);
            entity.Property(note => note.Id).ValueGeneratedNever();
            entity.HasIndex(note => note.AuthorId);
        });
        modelBuilder.Entity<StepRecord>(entity =>
        {
            entity.ToTable("steps");
            entity.HasKey(step => step.Id);
            entity.HasIndex(step => step.WhoId);
        });
        modelBuilder.Entity<BlockRecord>(entity =>
        {
            entity.ToTable("blocks");
            entity.HasKey(block => block.InstallId);
        });
        modelBuilder.Entity<UploadAskRecord>(entity =>
        {
            entity.ToTable("upload_asks");
            entity.HasKey(ask => ask.Id);
            entity.HasIndex(ask => new { ask.InstallId, ask.At });
        });
        modelBuilder.Entity<HandledRequestRecord>(entity =>
        {
            entity.ToTable("handled_requests");
            entity.HasKey(request => request.Id);
            entity.Property(request => request.Id).HasMaxLength(QueuedRequest.LongestId);
            entity.HasIndex(request => request.HandledAt);
        });
    }
}
