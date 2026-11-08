namespace GSBC.ChurchApp.Features.Notifications.Api.Data;

public sealed class NotificationRecord
{
    public Guid Id { get; init; }
    public required string Kind { get; init; }
    public required string Area { get; init; }
    public required string Title { get; init; }
    public string Body { get; init; } = "";
    public string? Link { get; init; }
    public string? Subject { get; init; }
    public bool Urgent { get; init; }
    public string? ScopeId { get; init; }
    public bool Test { get; init; }
    public bool Waits { get; init; }
    public bool InAdminPanel { get; init; }
    public string? Action { get; init; }
    public DateTime RaisedAt { get; init; }
    public DateTime? SettledAt { get; set; }
    public Guid? OneOf { get; init; }
    public int Rank { get; init; }
}

public sealed class InboxEntry
{
    public Guid Id { get; init; }
    public Guid NotificationId { get; init; }
    public NotificationRecord? Notification { get; init; }
    public Guid UserId { get; init; }
    public DateTime? ReadAt { get; set; }
}

public sealed class Device
{
    public Guid Id { get; init; }
    public Guid InstallId { get; init; }
    public Guid? UserId { get; set; }
    public required string Platform { get; init; }
    public required string AppId { get; set; }
    public required string Environment { get; set; }
    public required string Token { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastNudgedAt { get; init; }
}

public sealed class Preference
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public Guid? InstallId { get; init; }
    public required string Kind { get; init; }
    public string? ScopeId { get; init; }
    public bool Push { get; set; }
}

public sealed class Delivery
{
    public long Id { get; init; }
    public Guid DeviceId { get; init; }
    public Guid? NotificationId { get; init; }
    public NotificationRecord? Notification { get; init; }
    public Guid? UserId { get; init; }
    public DeliveryKind Kind { get; init; }
    public string? PacingGroup { get; init; }
    public bool Summarised { get; init; }
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
    public Guid Id { get; init; }
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
    public Guid AuthorId { get; init; }
    public DateTime UpdatedAt { get; set; }
    public int ReachedPeople { get; set; }
    public int ReachedPhones { get; set; }
    public Guid? NotificationId { get; set; }
}

public enum AnnouncementState
{
    Draft,
    WaitingToSend,
    Scheduled,
    Sent,
    Cancelled,
}
