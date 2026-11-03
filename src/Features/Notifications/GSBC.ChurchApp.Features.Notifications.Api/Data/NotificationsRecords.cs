namespace GSBC.ChurchApp.Features.Notifications.Api.Data;

public sealed class NotificationRecord
{
    public Guid Id { get; set; }
    public required string Kind { get; set; }
    public required string Area { get; set; }
    public required string Title { get; set; }
    public string Body { get; set; } = "";
    public string? Link { get; set; }
    public string? Subject { get; set; }
    public bool Urgent { get; set; }
    public string? ScopeId { get; set; }
    public bool Test { get; set; }
    public DateTime RaisedAt { get; set; }
}

public sealed class InboxEntry
{
    public Guid Id { get; set; }
    public Guid NotificationId { get; set; }
    public NotificationRecord? Notification { get; set; }
    public Guid UserId { get; set; }
    public DateTime? ReadAt { get; set; }
}

public sealed class Device
{
    public Guid Id { get; set; }
    public Guid InstallId { get; set; }
    public Guid? UserId { get; set; }
    public required string Platform { get; set; }
    public required string AppId { get; set; }
    public required string Environment { get; set; }
    public required string Token { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastNudgedAt { get; set; }
}

public sealed class Preference
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? InstallId { get; set; }
    public required string Kind { get; set; }
    public string? ScopeId { get; set; }
    public bool Push { get; set; }
}

public sealed class Delivery
{
    public long Id { get; set; }
    public Guid DeviceId { get; set; }
    public Guid? NotificationId { get; set; }
    public NotificationRecord? Notification { get; set; }
    public Guid? UserId { get; set; }
    public DeliveryKind Kind { get; set; }
    public string? PacingGroup { get; set; }
    public bool Summarised { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public int Attempts { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? GaveUpAt { get; set; }
    public string? LastError { get; set; }
}

public enum DeliveryKind
{
    Visible,
    Background,
}

public sealed class AnnouncementRecord
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public string Body { get; set; } = "";
    public string? Link { get; set; }
    public required string AudienceKind { get; set; }
    public string? AudienceNode { get; set; }
    public string? AudienceObjectId { get; set; }
    public Guid? AudienceUserId { get; set; }
    public bool Urgent { get; set; }
    public AnnouncementState State { get; set; }
    public DateTime? SendAt { get; set; }
    public DateTime? SentAt { get; set; }
    public Guid AuthorId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ReachedPeople { get; set; }
    public int ReachedPhones { get; set; }
}

public enum AnnouncementState
{
    Draft,
    WaitingToSend,
    Scheduled,
    Sent,
    Cancelled,
}
