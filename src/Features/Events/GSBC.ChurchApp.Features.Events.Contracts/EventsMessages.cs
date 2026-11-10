using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Events.Contracts;

[ProtoContract]
public sealed class Done;

public enum Repeats
{
    Weekly = 0,
    EverySecondWeek = 1,
    MonthlyByWeekday = 2,
    MonthlyByDate = 3,
    OnDates = 4,
}

public enum Showing
{
    SavedForLater = 0,
    Shown = 1,
    ShownFrom = 2,
}

public enum OccurrenceKind
{
    OneOff = 0,
    Regular = 1,
    Special = 2,
}

[ProtoContract]
public sealed class EventPlace
{
    [ProtoMember(1)] public string? PlaceId { get; set; }
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Address { get; set; } = "";
}

[ProtoContract]
public sealed class EventContact
{
    [ProtoMember(1)] public string? PersonId { get; set; }
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Phone { get; set; } = "";
    [ProtoMember(4)] public string Email { get; set; } = "";
}

[ProtoContract]
public sealed class EventLink
{
    [ProtoMember(1)] public string Title { get; set; } = "";
    [ProtoMember(2)] public string Url { get; set; } = "";
}

[ProtoContract]
public sealed class EventFields
{
    [ProtoMember(1)] public string Title { get; set; } = "";
    [ProtoMember(2)] public string Label { get; set; } = "";
    [ProtoMember(3)] public MediaRef? Picture { get; set; }
    [ProtoMember(4)] public RichText? Description { get; set; }
    [ProtoMember(5)] public EventPlace? Place { get; set; }
    [ProtoMember(6)] public List<EventContact> Contacts { get; set; } = [];
    [ProtoMember(7)] public string RegisterLink { get; set; } = "";
    [ProtoMember(8)] public List<EventLink> Links { get; set; } = [];
}

[ProtoContract]
public sealed class EventTimes
{
    [ProtoMember(1)] public string StartDate { get; set; } = "";
    [ProtoMember(2)] public string EndDate { get; set; } = "";
    [ProtoMember(3)] public int? StartMinute { get; set; }
    [ProtoMember(4)] public int? EndMinute { get; set; }
}

[ProtoContract]
public sealed class RegularPattern
{
    [ProtoMember(1)] public Repeats Repeats { get; set; }
    [ProtoMember(2)] public List<DayOfWeek> Days { get; set; } = [];
    [ProtoMember(3)] public int WeekOfMonth { get; set; }
    [ProtoMember(4)] public int DayOfMonth { get; set; }
    [ProtoMember(5)] public List<string> Dates { get; set; } = [];
    [ProtoMember(6)] public string StartDate { get; set; } = "";
    [ProtoMember(7)] public string EndDate { get; set; } = "";
}

[ProtoContract]
public sealed class CalendarInfo
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Slug { get; set; } = "";
    [ProtoMember(4)] public string Description { get; set; } = "";
    [ProtoMember(5)] public string Audience { get; set; } = "";
    [ProtoMember(6)] public bool ShownOnNewPhones { get; set; }
    [ProtoMember(7)] public bool ReadOnly { get; set; }
    [ProtoMember(8)] public string? MinistryId { get; set; }
    [ProtoMember(9)] public int Order { get; set; }
}

[ProtoContract]
public sealed class EventOccurrence
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public OccurrenceKind Kind { get; set; }
    [ProtoMember(3)] public string ItemId { get; set; } = "";
    [ProtoMember(4)] public string Date { get; set; } = "";
    [ProtoMember(5)] public string EndDate { get; set; } = "";
    [ProtoMember(6)] public string CalendarId { get; set; } = "";
    [ProtoMember(7)] public List<string> AlsoOn { get; set; } = [];
    [ProtoMember(8, IsRequired = true)] public EventFields Fields { get; set; } = new();
    [ProtoMember(9)] public DateTime StartsAt { get; set; }
    [ProtoMember(10)] public DateTime EndsAt { get; set; }
    [ProtoMember(11)] public bool AllDay { get; set; }
    [ProtoMember(12)] public string Repeats { get; set; } = "";
    [ProtoMember(13)] public bool NotOn { get; set; }
    [ProtoMember(14)] public string NotOnReason { get; set; } = "";
    [ProtoMember(15)] public string BackOn { get; set; } = "";
    [ProtoMember(16)] public List<string> Missing { get; set; } = [];
    [ProtoMember(17)] public DateTime? ChangedAt { get; set; }
    [ProtoMember(18)] public string ChangeNote { get; set; } = "";
    [ProtoMember(19)] public Showing Showing { get; set; }
    [ProtoMember(20)] public DateTime? ShowFrom { get; set; }
    [ProtoMember(21)] public bool Asked { get; set; }
    [ProtoMember(22)] public bool ListEveryDate { get; set; }
}

[ProtoContract]
public sealed class OccurrenceContacts
{
    [ProtoMember(1)] public string OccurrenceId { get; set; } = "";
    [ProtoMember(2)] public List<EventContact> Contacts { get; set; } = [];
}

[ProtoContract]
public sealed class CalendarChoice
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public bool InYourWeek { get; set; }
}

[ProtoContract]
public sealed class MyCalendars
{
    [ProtoMember(1)] public List<string> Added { get; set; } = [];
    [ProtoMember(2)] public List<string> TurnedOff { get; set; } = [];
}

[ProtoContract]
public sealed class CalendarSettings
{
    [ProtoMember(1, IsRequired = true)] public CalendarInfo Calendar { get; set; } = new();
    [ProtoMember(2)] public bool NeedsPublisher { get; set; }
    [ProtoMember(3)] public string ICalSource { get; set; } = "";
    [ProtoMember(4)] public DateTime? ICalReadAt { get; set; }
    [ProtoMember(5)] public string ICalProblem { get; set; } = "";
}

[ProtoContract]
public sealed class DeskCalendar
{
    [ProtoMember(1, IsRequired = true)] public CalendarSettings Settings { get; set; } = new();
    [ProtoMember(2)] public bool CanEdit { get; set; }
    [ProtoMember(3)] public bool CanPublish { get; set; }
}

[ProtoContract]
public sealed class CalendarDesk
{
    [ProtoMember(1)] public List<DeskCalendar> Calendars { get; set; } = [];
    [ProtoMember(2)] public bool CanManage { get; set; }
    [ProtoMember(3)] public int NeedsDetails { get; set; }
    [ProtoMember(4)] public int AskedToPublish { get; set; }
    [ProtoMember(5)] public List<ShowingRequest> AskedToShow { get; set; } = [];
}

[ProtoContract]
public sealed class ShowingOn
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public bool Agreed { get; set; }
    [ProtoMember(3)] public bool AskedByOther { get; set; }
}

[ProtoContract]
public sealed class ShowingRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string HomeCalendarId { get; set; } = "";
    [ProtoMember(3)] public string? EventId { get; set; }
    [ProtoMember(4)] public string? RegularEventId { get; set; }
    [ProtoMember(5)] public string Title { get; set; } = "";
    [ProtoMember(6)] public DateTime AskedAt { get; set; }
    [ProtoMember(7)] public bool Widens { get; set; }
    [ProtoMember(8)] public string NewlyReaches { get; set; } = "";
    [ProtoMember(9)] public List<string> NewlyReachesRoleIds { get; set; } = [];
    [ProtoMember(10)] public bool AskedByOther { get; set; }
    [ProtoMember(11)] public string CalendarName { get; set; } = "";
    [ProtoMember(12)] public string HomeCalendarName { get; set; } = "";
}

[ProtoContract]
public sealed class EventRef : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string EventId { get; set; } = "";

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class EventDetail
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string CalendarId { get; set; } = "";
    [ProtoMember(3, IsRequired = true)] public EventFields Fields { get; set; } = new();
    [ProtoMember(4, IsRequired = true)] public EventTimes Times { get; set; } = new();
    [ProtoMember(5)] public Showing Showing { get; set; }
    [ProtoMember(6)] public DateTime? ShowFrom { get; set; }
    [ProtoMember(7)] public bool Asked { get; set; }
    [ProtoMember(8)] public List<ShowingOn> AlsoOn { get; set; } = [];
    [ProtoMember(9)] public string ChangeNote { get; set; } = "";
    [ProtoMember(10)] public DateTime? ChangedAt { get; set; }
    [ProtoMember(11)] public bool NotOn { get; set; }
    [ProtoMember(12)] public string NotOnNote { get; set; } = "";
    [ProtoMember(13)] public DateTime UpdatedAt { get; set; }
    [ProtoMember(14)] public bool ReadOnly { get; set; }
}

[ProtoContract]
public sealed class EventSave : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string EventId { get; set; } = "";
    [ProtoMember(3, IsRequired = true)] public EventFields Fields { get; set; } = new();
    [ProtoMember(4, IsRequired = true)] public EventTimes Times { get; set; } = new();
    [ProtoMember(5)] public Showing Showing { get; set; }
    [ProtoMember(6)] public DateTime? ShowFrom { get; set; }
    [ProtoMember(7)] public bool NotOn { get; set; }
    [ProtoMember(8)] public string NotOnNote { get; set; } = "";
    [ProtoMember(9)] public string NewId { get; set; } = "";
    [ProtoMember(10)] public EventSave? Base { get; set; }

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class RegularRef : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string RegularEventId { get; set; } = "";

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class BreakInfo
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string StartDate { get; set; } = "";
    [ProtoMember(4)] public string EndDate { get; set; } = "";
    [ProtoMember(5)] public List<string> UsedBy { get; set; } = [];
}

[ProtoContract]
public sealed class BreakList
{
    [ProtoMember(1)] public List<BreakInfo> Breaks { get; set; } = [];
}

[ProtoContract]
public sealed class BreakRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class SpecialInfo
{
    [ProtoMember(1)] public string Date { get; set; } = "";
    [ProtoMember(2)] public List<string> Changed { get; set; } = [];
    [ProtoMember(3, IsRequired = true)] public EventFields Fields { get; set; } = new();
    [ProtoMember(4)] public int? StartMinute { get; set; }
    [ProtoMember(5)] public int? EndMinute { get; set; }
    [ProtoMember(6)] public bool NotOn { get; set; }
    [ProtoMember(7)] public string NotOnNote { get; set; } = "";
    [ProtoMember(8)] public string ChangeNote { get; set; } = "";
    [ProtoMember(9)] public DateTime? ChangedAt { get; set; }
}

[ProtoContract]
public sealed class RegularEventDetail
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string CalendarId { get; set; } = "";
    [ProtoMember(3, IsRequired = true)] public EventFields Fields { get; set; } = new();
    [ProtoMember(4, IsRequired = true)] public RegularPattern Pattern { get; set; } = new();
    [ProtoMember(5)] public int? StartMinute { get; set; }
    [ProtoMember(6)] public int? EndMinute { get; set; }
    [ProtoMember(7)] public Showing Showing { get; set; }
    [ProtoMember(8)] public DateTime? ShowFrom { get; set; }
    [ProtoMember(9)] public bool Asked { get; set; }
    [ProtoMember(10)] public List<string> Needs { get; set; } = [];
    [ProtoMember(11)] public List<int> RemindDaysBefore { get; set; } = [];
    [ProtoMember(12)] public List<string> BreakIds { get; set; } = [];
    [ProtoMember(13)] public List<BreakInfo> OwnBreaks { get; set; } = [];
    [ProtoMember(14)] public List<SpecialInfo> Specials { get; set; } = [];
    [ProtoMember(15)] public string Repeats { get; set; } = "";
    [ProtoMember(16)] public List<ShowingOn> AlsoOn { get; set; } = [];
    [ProtoMember(17)] public string ChangeNote { get; set; } = "";
    [ProtoMember(18)] public DateTime? ChangedAt { get; set; }
    [ProtoMember(19)] public DateTime UpdatedAt { get; set; }
    [ProtoMember(21)] public List<RegularDate> Dates { get; set; } = [];
    [ProtoMember(22)] public bool ListEveryDate { get; set; }
}

[ProtoContract]
public sealed class RegularDate
{
    [ProtoMember(1)] public string Date { get; set; } = "";
    [ProtoMember(2)] public string OccurrenceId { get; set; } = "";
    [ProtoMember(3)] public string Title { get; set; } = "";
    [ProtoMember(4)] public int? StartMinute { get; set; }
    [ProtoMember(5)] public int? EndMinute { get; set; }
    [ProtoMember(6)] public bool Changed { get; set; }
    [ProtoMember(7)] public bool NotOn { get; set; }
    [ProtoMember(8)] public string NotOnReason { get; set; } = "";
}

[ProtoContract]
public sealed class CalendarRef : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class RegularEventSummary
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string CalendarId { get; set; } = "";
    [ProtoMember(3)] public string Title { get; set; } = "";
    [ProtoMember(4)] public string Repeats { get; set; } = "";
    [ProtoMember(5)] public int? StartMinute { get; set; }
    [ProtoMember(6)] public int? EndMinute { get; set; }
    [ProtoMember(7)] public string NextDate { get; set; } = "";
    [ProtoMember(8)] public int ChangedDates { get; set; }
    [ProtoMember(9)] public Showing Showing { get; set; }
    [ProtoMember(10)] public bool Asked { get; set; }
    [ProtoMember(11)] public bool Ended { get; set; }
}

[ProtoContract]
public sealed class RegularEventList
{
    [ProtoMember(1)] public List<RegularEventSummary> RegularEvents { get; set; } = [];
}

[ProtoContract]
public sealed class RegularEventSave : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string RegularEventId { get; set; } = "";
    [ProtoMember(3, IsRequired = true)] public EventFields Fields { get; set; } = new();
    [ProtoMember(4, IsRequired = true)] public RegularPattern Pattern { get; set; } = new();
    [ProtoMember(5)] public int? StartMinute { get; set; }
    [ProtoMember(6)] public int? EndMinute { get; set; }
    [ProtoMember(7)] public Showing Showing { get; set; }
    [ProtoMember(8)] public DateTime? ShowFrom { get; set; }
    [ProtoMember(9)] public List<string> Needs { get; set; } = [];
    [ProtoMember(10)] public List<int> RemindDaysBefore { get; set; } = [];
    [ProtoMember(11)] public List<string> BreakIds { get; set; } = [];
    [ProtoMember(12)] public List<BreakInfo> OwnBreaks { get; set; } = [];
    [ProtoMember(13)] public bool KeepOwnConfirmed { get; set; }
    [ProtoMember(14)] public bool ListEveryDate { get; set; }
    [ProtoMember(15)] public string NewId { get; set; } = "";
    [ProtoMember(16)] public RegularEventSave? Base { get; set; }

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class KeptValue
{
    [ProtoMember(1)] public string Date { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public string Field { get; set; } = "";
    [ProtoMember(4)] public string Value { get; set; } = "";
}

[ProtoContract]
public sealed class RegularEventSaved
{
    [ProtoMember(1)] public bool Saved { get; set; }
    [ProtoMember(2)] public RegularEventDetail? Detail { get; set; }
    [ProtoMember(3)] public List<KeptValue> KeepOwn { get; set; } = [];
}

[ProtoContract]
public sealed class DateSave : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string RegularEventId { get; set; } = "";
    [ProtoMember(3)] public string Date { get; set; } = "";
    [ProtoMember(4, IsRequired = true)] public EventFields Fields { get; set; } = new();
    [ProtoMember(5)] public int? StartMinute { get; set; }
    [ProtoMember(6)] public int? EndMinute { get; set; }
    [ProtoMember(7)] public bool NotOn { get; set; }
    [ProtoMember(8)] public string NotOnNote { get; set; } = "";
    [ProtoMember(9)] public DateSave? Base { get; set; }

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class ItemRef : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string? EventId { get; set; }
    [ProtoMember(3)] public string? RegularEventId { get; set; }

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class ShowOnRequest : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string? EventId { get; set; }
    [ProtoMember(3)] public string? RegularEventId { get; set; }
    [ProtoMember(4)] public string OtherCalendarId { get; set; } = "";
    [ProtoMember(5)] public bool Show { get; set; }

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class ShowableQuery : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string Search { get; set; } = "";

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class ShowableItem
{
    [ProtoMember(1)] public string HomeCalendarId { get; set; } = "";
    [ProtoMember(2)] public string HomeCalendarName { get; set; } = "";
    [ProtoMember(3)] public string? EventId { get; set; }
    [ProtoMember(4)] public string? RegularEventId { get; set; }
    [ProtoMember(5)] public string Title { get; set; } = "";
    [ProtoMember(6)] public string When { get; set; } = "";
    [ProtoMember(7)] public bool Published { get; set; }
    [ProtoMember(8)] public bool Asked { get; set; }
    [ProtoMember(9)] public bool AgreesAtOnce { get; set; }
    [ProtoMember(10)] public bool Widens { get; set; }
    [ProtoMember(11)] public string NewlyReaches { get; set; } = "";
    [ProtoMember(12)] public List<string> NewlyReachesRoleIds { get; set; } = [];
}

[ProtoContract]
public sealed class ShowableList
{
    [ProtoMember(1)] public List<ShowableItem> Items { get; set; } = [];
}

[ProtoContract]
public sealed class ShowHereRequest : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string HomeCalendarId { get; set; } = "";
    [ProtoMember(3)] public string? EventId { get; set; }
    [ProtoMember(4)] public string? RegularEventId { get; set; }
    [ProtoMember(5)] public bool Show { get; set; }

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class ShowHereResult
{
    [ProtoMember(1)] public bool Agreed { get; set; }
}

[ProtoContract]
public sealed class AskToShowAnswer : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string OtherCalendarId { get; set; } = "";
    [ProtoMember(3)] public string? EventId { get; set; }
    [ProtoMember(4)] public string? RegularEventId { get; set; }
    [ProtoMember(5)] public bool Agree { get; set; }

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class ShowingAnswer : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string? EventId { get; set; }
    [ProtoMember(3)] public string? RegularEventId { get; set; }
    [ProtoMember(4)] public bool Agree { get; set; }

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class TellPeople : IScopedRequest
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string OccurrenceId { get; set; } = "";
    [ProtoMember(3)] public string Title { get; set; } = "";
    [ProtoMember(4)] public string Body { get; set; } = "";

    public string ScopeId => CalendarId;
}

[ProtoContract]
public sealed class AgendaQuery
{
    [ProtoMember(1)] public List<string> CalendarIds { get; set; } = [];
    [ProtoMember(2)] public string From { get; set; } = "";
    [ProtoMember(3)] public string To { get; set; } = "";
}

[ProtoContract]
public sealed class Agenda
{
    [ProtoMember(1)] public List<EventOccurrence> Occurrences { get; set; } = [];
}

[ProtoContract]
public sealed class NeedsDetailsItem
{
    [ProtoMember(1, IsRequired = true)] public EventOccurrence Occurrence { get; set; } = new();
    [ProtoMember(2)] public int DaysToGo { get; set; }
}

[ProtoContract]
public sealed class NeedsDetailsList
{
    [ProtoMember(1)] public List<NeedsDetailsItem> Items { get; set; } = [];
}

[ProtoContract]
public sealed class AskedItem
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string? EventId { get; set; }
    [ProtoMember(3)] public string? RegularEventId { get; set; }
    [ProtoMember(4)] public string Title { get; set; } = "";
    [ProtoMember(5)] public DateTime AskedAt { get; set; }
    [ProtoMember(6)] public string? AskedBy { get; set; }
}

[ProtoContract]
public sealed class AskedList
{
    [ProtoMember(1)] public List<AskedItem> Items { get; set; } = [];
}

[ProtoContract]
public sealed class PlacesUsed
{
    [ProtoMember(1)] public List<EventPlace> Places { get; set; } = [];
}

[ProtoContract]
public sealed class CalendarToRemove
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
}

[ProtoContract]
public sealed class CalendarRemoval
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public int Events { get; set; }
    [ProtoMember(3)] public int RegularEvents { get; set; }
    [ProtoMember(4)] public int ShownElsewhere { get; set; }
    [ProtoMember(5)] public int FromElsewhere { get; set; }
    [ProtoMember(6)] public int AddedBy { get; set; }
    [ProtoMember(7)] public bool ShownOnNewPhones { get; set; }
    [ProtoMember(8)] public bool ForEveryone { get; set; }
}

[ProtoContract]
public sealed class EditingCalendar
{
    [ProtoMember(1, IsRequired = true)] public CalendarSettings Settings { get; set; } = new();
    [ProtoMember(2)] public List<BreakInfo> Breaks { get; set; } = [];
    [ProtoMember(3)] public List<ShowingRequest> Asked { get; set; } = [];
}

[ProtoContract]
public sealed class EditingRegular
{
    [ProtoMember(1, IsRequired = true)] public RegularEventDetail Detail { get; set; } = new();
    [ProtoMember(2, IsRequired = true)] public RegularEventSummary Summary { get; set; } = new();
}

[ProtoContract]
public sealed class QueuedEdit
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public string EntryId { get; set; } = "";
    [ProtoMember(3)] public string Method { get; set; } = "";
    [ProtoMember(4)] public byte[] Request { get; set; } = [];
}

[ProtoContract]
public sealed class QueuedEdits
{
    [ProtoMember(1)] public List<QueuedEdit> Edits { get; set; } = [];
}
