using GSBC.ChurchApp.Features.Feedback.Contracts;

namespace GSBC.ChurchApp.Features.Feedback.Api.Data;

public sealed class TicketRecord
{
    public Guid Id { get; set; }
    public int Number { get; set; }
    public string Text { get; set; } = "";
    public DateTime SentAt { get; set; }
    public DateTime ChangedAt { get; set; }
    public TicketStatus Status { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Guid? InstallId { get; set; }
    public Guid? UserId { get; set; }
    public string SenderName { get; set; } = "";
    public string AppVersion { get; set; } = "";
    public string AppBuild { get; set; } = "";
    public string Platform { get; set; } = "";
    public string OsVersion { get; set; } = "";
    public string Model { get; set; } = "";
    public bool DevelopmentBuild { get; set; }
    public string Environment { get; set; } = "";
    public string Page { get; set; } = "";
    public string State { get; set; } = "";
    public byte[]? PhoneData { get; set; }
    public bool HadPhoneData { get; set; }
    public bool AttachmentsDeleted { get; set; }
    public FeedbackSource Source { get; set; }
    public string Browser { get; set; } = "";
    public string BrowserSystem { get; set; } = "";
    public string Window { get; set; } = "";
    public string ColourScheme { get; set; } = "";
    public string PageTitle { get; set; } = "";
    public string RecentPages { get; set; } = "";
    public string Screenshots { get; set; } = "";
    public List<AttachmentRecord> Attachments { get; set; } = [];
    public List<NoteRecord> Notes { get; set; } = [];
    public List<StepRecord> Steps { get; set; } = [];
}

public sealed class AttachmentRecord
{
    public Guid TicketId { get; set; }
    public int Position { get; set; }
    public required string MediaId { get; set; }
    public required byte[] Media { get; set; }
}

public sealed class NoteRecord
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid? AuthorId { get; set; }
    public string Text { get; set; } = "";
    public DateTime At { get; set; }
}

public sealed class StepRecord
{
    public long Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid? WhoId { get; set; }
    public string What { get; set; } = "";
    public DateTime At { get; set; }
}

public sealed class BlockRecord
{
    public Guid InstallId { get; set; }
    public string Reason { get; set; } = "";
    public Guid? BlockedBy { get; set; }
    public DateTime BlockedAt { get; set; }
}

public sealed class UploadAskRecord
{
    public long Id { get; set; }
    public Guid InstallId { get; set; }
    public DateTime At { get; set; }
}

public sealed class HandledRequestRecord
{
    public required string Id { get; set; }
    public DateTime HandledAt { get; set; }
}
