namespace GSBC.ChurchApp.Features.Chats.Api.Data;

public sealed class ChatRecord
{
    public Guid Id { get; set; }
    public Guid MinistryId { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Audience { get; set; } = "";
    public Guid? AskerId { get; set; }
    public string AskedAs { get; set; } = "";
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public long LastSeq { get; set; }
    public List<string> Flags { get; set; } = [];
    public bool HasMinors { get; set; }
}

public sealed class MessageRecord
{
    public Guid Id { get; set; }
    public Guid ChatId { get; set; }
    public long Seq { get; set; }
    public string Kind { get; set; } = "";
    public Guid? SenderId { get; set; }
    public Guid? ForPersonId { get; set; }
    public string ClientId { get; set; } = "";
    public DateTime SentAt { get; set; }
    public string Text { get; set; } = "";
    public Guid? ReplyToId { get; set; }
    public List<Guid> Mentions { get; set; } = [];
    public List<StoredAttachment> Attachments { get; set; } = [];
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}

public sealed class StoredAttachment
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Bytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public sealed class MessageVersionRecord
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public int Number { get; set; }
    public string Action { get; set; } = "";
    public string Text { get; set; } = "";
    public List<StoredAttachment> Attachments { get; set; } = [];
    public DateTime At { get; set; }
    public Guid? ById { get; set; }
}

public sealed class ReactionRecord
{
    public Guid MessageId { get; set; }
    public Guid PersonId { get; set; }
    public string Emoji { get; set; } = "";
    public DateTime At { get; set; }
}

public sealed class ParticipantRecord
{
    public Guid ChatId { get; set; }
    public Guid PersonId { get; set; }
    public string Role { get; set; } = "";
    public string State { get; set; } = "";
    public List<Guid> ForChildren { get; set; } = [];
    public DateTime Since { get; set; }
    public long ReadSeq { get; set; }
    public bool Muted { get; set; }
}

public sealed class ConsentRecord
{
    public Guid ChatId { get; set; }
    public Guid ChildId { get; set; }
    public bool? Approved { get; set; }
    public Guid? DecidedBy { get; set; }
    public DateTime? DecidedAt { get; set; }
    public DateTime? AskedAt { get; set; }
}

public sealed class ReportRecord
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public Guid ChatId { get; set; }
    public Guid? ReporterId { get; set; }
    public string To { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime At { get; set; }
    public DateTime? HandledAt { get; set; }
    public Guid? HandledBy { get; set; }
    public string Outcome { get; set; } = "";
}

public sealed class AuditRecord
{
    public Guid Id { get; set; }
    public DateTime At { get; set; }
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = "";
    public Guid? ChatId { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? PersonId { get; set; }
    public string Words { get; set; } = "";
}

public static class AuditActions
{
    public const string Started = "started";
    public const string Changed = "changed";
    public const string Deleted = "deleted";
    public const string PeopleChanged = "people";
    public const string Sent = "sent";
    public const string Edited = "edited";
    public const string MessageDeleted = "message-deleted";
    public const string Consent = "consent";
    public const string Reported = "reported";
    public const string Handled = "handled";
    public const string Checked = "checked";
    public const string Paused = "paused";
    public const string Unpaused = "unpaused";
    public const string Forgotten = "forgotten";
}
