using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Feedback.Contracts;

[ProtoContract]
public sealed class Done;

public enum TicketStatus
{
    New = 0,
    InProgress = 1,
    FixedToCheck = 2,
    Resolved = 3,
    NotDoing = 4,
}

public enum FeedbackSource
{
    App = 0,
    AdminPanel = 1,
}

public enum SourceFilter
{
    Both = 0,
    App = 1,
    AdminPanel = 2,
}

public enum DeskFilter
{
    Open = 0,
    Closed = 1,
    Everything = 2,
}

[ProtoContract]
public sealed class PhoneDetails
{
    [ProtoMember(1)] public string AppVersion { get; set; } = "";
    [ProtoMember(2)] public string AppBuild { get; set; } = "";
    [ProtoMember(3)] public string Platform { get; set; } = "";
    [ProtoMember(4)] public string OsVersion { get; set; } = "";
    [ProtoMember(5)] public string Model { get; set; } = "";
    [ProtoMember(6)] public bool DevelopmentBuild { get; set; }
}

[ProtoContract]
public sealed class BrowserDetails
{
    [ProtoMember(1)] public string Browser { get; set; } = "";
    [ProtoMember(2)] public string System { get; set; } = "";
    [ProtoMember(3)] public string Window { get; set; } = "";
    [ProtoMember(4)] public string ColourScheme { get; set; } = "";
    [ProtoMember(5)] public string Version { get; set; } = "";
    [ProtoMember(6)] public string PageTitle { get; set; } = "";
    [ProtoMember(7)] public List<string> RecentPages { get; set; } = [];
}

[ProtoContract]
public sealed class FeedbackSend
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(FeedbackAreas.AttachmentIdsInSend)] public List<string> AttachmentIds { get; set; } = [];
    [ProtoMember(FeedbackAreas.PhoneDataIdInSend)] public string PhoneDataId { get; set; } = "";
    [ProtoMember(5)] public PhoneDetails? Phone { get; set; }
    [ProtoMember(6)] public string Page { get; set; } = "";
    [ProtoMember(7)] public string State { get; set; } = "";
    [ProtoMember(8)] public FeedbackSource Source { get; set; }
    [ProtoMember(9)] public BrowserDetails? Browser { get; set; }
}

[ProtoContract]
public sealed class DeskQuery
{
    [ProtoMember(1)] public DeskFilter Filter { get; set; }
    [ProtoMember(2)] public string Search { get; set; } = "";
    [ProtoMember(3)] public SourceFilter Source { get; set; }
}

[ProtoContract]
public sealed class DeskList
{
    [ProtoMember(1)] public List<TicketLine> Tickets { get; set; } = [];
    [ProtoMember(2)] public List<StatusCount> Counts { get; set; } = [];
}

[ProtoContract]
public sealed class StatusCount
{
    [ProtoMember(1)] public TicketStatus Status { get; set; }
    [ProtoMember(2)] public int Count { get; set; }
}

[ProtoContract]
public sealed class TicketLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public int Number { get; set; }
    [ProtoMember(3)] public string Text { get; set; } = "";
    [ProtoMember(4)] public string From { get; set; } = "";
    [ProtoMember(5)] public DateTime SentAt { get; set; }
    [ProtoMember(6)] public TicketStatus Status { get; set; }
    [ProtoMember(7)] public int Attachments { get; set; }
    [ProtoMember(8)] public MediaRef? Cover { get; set; }
    [ProtoMember(9)] public string AppBuild { get; set; } = "";
    [ProtoMember(10)] public string Model { get; set; } = "";
    [ProtoMember(11)] public DateTime ChangedAt { get; set; }
    [ProtoMember(12)] public bool AttachmentsDeleted { get; set; }
    [ProtoMember(13)] public FeedbackSource Source { get; set; }
    [ProtoMember(14)] public string Browser { get; set; } = "";
    [ProtoMember(15)] public DateTime? FilesGoAt { get; set; }
}

[ProtoContract]
public sealed class TicketRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public int Number { get; set; }
}

[ProtoContract]
public sealed class Ticket
{
    [ProtoMember(1, IsRequired = true)] public TicketLine Line { get; set; } = new();
    [ProtoMember(2)] public List<MediaRef> Attachments { get; set; } = [];
    [ProtoMember(3)] public PhoneDetails? Phone { get; set; }
    [ProtoMember(4)] public string Environment { get; set; } = "";
    [ProtoMember(5)] public string Page { get; set; } = "";
    [ProtoMember(6)] public string State { get; set; } = "";
    [ProtoMember(7)] public string PersonId { get; set; } = "";
    [ProtoMember(8)] public string InstallId { get; set; } = "";
    [ProtoMember(9)] public int FromThisPhone { get; set; }
    [ProtoMember(10)] public string BlockedBecause { get; set; } = "";
    [ProtoMember(11)] public bool Blocked { get; set; }
    [ProtoMember(12)] public List<TicketNote> Notes { get; set; } = [];
    [ProtoMember(13)] public List<TicketStep> History { get; set; } = [];
    [ProtoMember(14)] public MediaRef? PhoneData { get; set; }
    [ProtoMember(15)] public bool HadPhoneData { get; set; }
    [ProtoMember(16)] public DateTime? ClosedAt { get; set; }
    [ProtoMember(17)] public BrowserDetails? Browser { get; set; }
}

[ProtoContract]
public sealed class TicketNote
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public string Author { get; set; } = "";
    [ProtoMember(4)] public DateTime At { get; set; }
}

[ProtoContract]
public sealed class TicketStep
{
    [ProtoMember(1)] public string What { get; set; } = "";
    [ProtoMember(2)] public string Who { get; set; } = "";
    [ProtoMember(3)] public DateTime At { get; set; }
}

[ProtoContract]
public sealed class StatusChange
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public TicketStatus Status { get; set; }
    [ProtoMember(3)] public string Note { get; set; } = "";
}

[ProtoContract]
public sealed class NoteSave
{
    [ProtoMember(1)] public string TicketId { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
}

[ProtoContract]
public sealed class NoteRef
{
    [ProtoMember(1)] public string TicketId { get; set; } = "";
    [ProtoMember(2)] public string NoteId { get; set; } = "";
}

[ProtoContract]
public sealed class BlockSave
{
    [ProtoMember(1)] public string TicketId { get; set; } = "";
    [ProtoMember(2)] public string Reason { get; set; } = "";
}

[ProtoContract]
public sealed class PhoneRef
{
    [ProtoMember(1)] public string InstallId { get; set; } = "";
}

[ProtoContract]
public sealed class BlockedQuery;

[ProtoContract]
public sealed class BlockedPhones
{
    [ProtoMember(1)] public List<BlockedPhone> Phones { get; set; } = [];
}

[ProtoContract]
public sealed class BlockedPhone
{
    [ProtoMember(1)] public string InstallId { get; set; } = "";
    [ProtoMember(2)] public string Reason { get; set; } = "";
    [ProtoMember(3)] public DateTime BlockedAt { get; set; }
    [ProtoMember(4)] public string BlockedBy { get; set; } = "";
    [ProtoMember(5)] public int Sent { get; set; }
    [ProtoMember(6)] public string Model { get; set; } = "";
    [ProtoMember(7)] public int LastNumber { get; set; }
}

[ProtoContract]
public sealed class SentOnPhone
{
    [ProtoMember(1)] public List<SentFeedback> Sent { get; set; } = [];
    [ProtoMember(2)] public string Refused { get; set; } = "";
}

[ProtoContract]
public sealed class SentFeedback
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string OutboxId { get; set; } = "";
    [ProtoMember(3)] public DateTime At { get; set; }
    [ProtoMember(4)] public int Attachments { get; set; }
}
