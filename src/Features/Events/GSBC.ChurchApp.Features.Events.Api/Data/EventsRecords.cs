using GSBC.ChurchApp.Features.Events.Contracts;

namespace GSBC.ChurchApp.Features.Events.Api.Data;

public sealed class CalendarRecord
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string Description { get; set; } = "";
    public string Audience { get; set; } = Platform.Contracts.Configuration.Audience.Everyone.Text;
    public bool ShownOnNewPhones { get; set; }
    public bool NeedsPublisher { get; set; }
    public Guid? MinistryId { get; set; }
    public string? ICalSource { get; set; }
    public DateTime? ICalReadAt { get; set; }
    public string? ICalProblem { get; set; }
    public string? ICalETag { get; set; }
    public string? ICalLastModified { get; set; }
    public DateOnly? ICalCopiedTo { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public interface IShowable
{
    Guid Id { get; }
    Guid CalendarId { get; set; }
    EventFields Fields { get; set; }
    Showing Showing { get; set; }
    DateTime? ShowFrom { get; set; }
    DateTime? AskedAt { get; set; }
    Guid? AskedBy { get; set; }
    string ChangeNote { get; set; }
    DateTime? ChangedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}

public sealed class EventRecord : IShowable
{
    public Guid Id { get; set; }
    public Guid CalendarId { get; set; }
    public EventFields Fields { get; set; } = new();
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int? StartMinute { get; set; }
    public int? EndMinute { get; set; }
    public Showing Showing { get; set; }
    public DateTime? ShowFrom { get; set; }
    public DateTime? AskedAt { get; set; }
    public Guid? AskedBy { get; set; }
    public bool NotOn { get; set; }
    public string NotOnNote { get; set; } = "";
    public string ChangeNote { get; set; } = "";
    public DateTime? ChangedAt { get; set; }
    public string? SourceUid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class RegularEventRecord : IShowable
{
    public Guid Id { get; set; }
    public Guid CalendarId { get; set; }
    public EventFields Fields { get; set; } = new();
    public RegularPattern Pattern { get; set; } = new();
    public int? StartMinute { get; set; }
    public int? EndMinute { get; set; }
    public Showing Showing { get; set; }
    public DateTime? ShowFrom { get; set; }
    public DateTime? AskedAt { get; set; }
    public Guid? AskedBy { get; set; }
    public List<string> Needs { get; set; } = [];
    public List<int> RemindDaysBefore { get; set; } = [];
    public string ChangeNote { get; set; } = "";
    public DateTime? ChangedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SpecialRecord
{
    public Guid RegularEventId { get; set; }
    public DateOnly Date { get; set; }
    public List<string> Changed { get; set; } = [];
    public EventFields Fields { get; set; } = new();
    public int? StartMinute { get; set; }
    public int? EndMinute { get; set; }
    public bool NotOn { get; set; }
    public string NotOnNote { get; set; } = "";
    public string ChangeNote { get; set; } = "";
    public DateTime? ChangedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class BreakRecord
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? RegularEventId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class RegularEventBreak
{
    public Guid RegularEventId { get; set; }
    public Guid BreakId { get; set; }
}

public sealed class ShowingRecord
{
    public Guid Id { get; set; }
    public Guid CalendarId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? RegularEventId { get; set; }
    public DateTime AskedAt { get; set; }
    public Guid? AskedBy { get; set; }
    public DateTime? AgreedAt { get; set; }
    public Guid? AgreedBy { get; set; }
    public bool AskedByOther { get; set; }
}

public sealed class PublishedRow
{
    public required string Feed { get; set; }
    public required string RecordId { get; set; }
    public required string Hash { get; set; }
    public string? Audience { get; set; }
}

public sealed class CalendarLinkRecord
{
    public required string Secret { get; set; }
    public Guid? UserId { get; set; }
    public Guid? InstallId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class DetailsReminderRecord
{
    public required string OccurrenceId { get; set; }
    public int DaysBefore { get; set; }
    public Guid RegularEventId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? SettledAt { get; set; }
}

public sealed class DigestRecord
{
    public DateOnly Start { get; set; }
    public int Weeks { get; set; }
    public List<Guid>? CalendarIds { get; set; }
    public List<DigestEntry> Entries { get; set; } = [];
    public SentDigest Sent { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? SentAt { get; set; }
    public Guid? SentBy { get; set; }
}

public sealed class SentDigest
{
    public DateOnly End { get; set; }
    public List<SentDigestItem> Items { get; set; } = [];
}

public sealed class SentDigestItem
{
    public DigestShownItem Item { get; set; } = new();
    public List<Guid> CalendarIds { get; set; } = [];
}
