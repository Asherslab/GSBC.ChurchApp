using ProtoBuf;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

[ProtoContract]
public sealed class Done;

public enum CareState
{
    New = 0,
    FollowedUp = 1,
    Answered = 2,
}

public enum WallState
{
    NotAsked = 0,
    Waiting = 1,
    Shown = 2,
    KeptPrivate = 3,
    Ended = 4,
    TakenDown = 5,
}

[ProtoContract]
public sealed class PrayerRequestSend
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public bool ForWall { get; set; }
    [ProtoMember(4)] public bool ShowName { get; set; }
    [ProtoMember(5)] public string Name { get; set; } = "";
    [ProtoMember(6)] public bool WantsContact { get; set; }
    [ProtoMember(7)] public string Contact { get; set; } = "";
}

[ProtoContract]
public sealed class RequestRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class PrayMark
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public bool Prayed { get; set; }
}

[ProtoContract]
public sealed class AnsweredSave
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Note { get; set; } = "";
}

[ProtoContract]
public sealed class WallPost
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public string Name { get; set; } = "";
    [ProtoMember(4)] public DateTime ShownAt { get; set; }
    [ProtoMember(5)] public DateTime Until { get; set; }
    [ProtoMember(6)] public int Prayed { get; set; }
    [ProtoMember(7)] public bool Answered { get; set; }
    [ProtoMember(8)] public string AnsweredNote { get; set; } = "";
}

[ProtoContract]
public sealed class YourRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public DateTime SentAt { get; set; }
    [ProtoMember(4)] public WallState Wall { get; set; }
    [ProtoMember(5)] public DateTime? WallUntil { get; set; }
    [ProtoMember(6)] public int Prayed { get; set; }
    [ProtoMember(7)] public bool Answered { get; set; }
    [ProtoMember(8)] public string AnsweredNote { get; set; } = "";
    [ProtoMember(9)] public string WallName { get; set; } = "";
    [ProtoMember(10)] public string WallText { get; set; } = "";
}

[ProtoContract]
public sealed class SentOnPhone
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public DateTime SentAt { get; set; }
    [ProtoMember(4)] public bool ForWall { get; set; }
    [ProtoMember(5)] public string OutboxId { get; set; } = "";
    [ProtoMember(6)] public bool SignedIn { get; set; }
}

[ProtoContract]
public sealed class PrayedOnPhone
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string OutboxId { get; set; } = "";
}

[ProtoContract]
public sealed class KeptOnPhone
{
    [ProtoMember(1)] public List<SentOnPhone> Sent { get; set; } = [];
    [ProtoMember(2)] public List<PrayedOnPhone> Prayed { get; set; } = [];
    [ProtoMember(3)] public List<QueuedOnPhone> Queued { get; set; } = [];
}

[ProtoContract]
public sealed class QueuedOnPhone
{
    [ProtoMember(1)] public string RequestId { get; set; } = "";
    [ProtoMember(2)] public string OutboxId { get; set; } = "";
    [ProtoMember(3)] public string What { get; set; } = "";
}

[ProtoContract]
public sealed class InboxRequest
{
    [ProtoMember(1)] public string Search { get; set; } = "";
}

[ProtoContract]
public sealed class Inbox
{
    [ProtoMember(1)] public List<InboxLine> Requests { get; set; } = [];
    [ProtoMember(2)] public bool MayRead { get; set; }
}

[ProtoContract]
public sealed class InboxLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public string From { get; set; } = "";
    [ProtoMember(4)] public DateTime SentAt { get; set; }
    [ProtoMember(5)] public CareState Care { get; set; }
    [ProtoMember(6)] public WallState Wall { get; set; }
    [ProtoMember(7)] public bool Minor { get; set; }
    [ProtoMember(8)] public bool WantsContact { get; set; }
    [ProtoMember(9)] public DateTime ChangedAt { get; set; }
}

[ProtoContract]
public sealed class RequestDetails
{
    [ProtoMember(1, IsRequired = true)] public InboxLine Line { get; set; } = new();
    [ProtoMember(2)] public bool SignedIn { get; set; }
    [ProtoMember(3)] public string PersonId { get; set; } = "";
    [ProtoMember(4)] public string Contact { get; set; } = "";
    [ProtoMember(5)] public bool ShowName { get; set; }
    [ProtoMember(6)] public string WallText { get; set; } = "";
    [ProtoMember(7)] public string WallName { get; set; } = "";
    [ProtoMember(8)] public DateTime? WallUntil { get; set; }
    [ProtoMember(9)] public int Prayed { get; set; }
    [ProtoMember(10)] public string AnsweredNote { get; set; } = "";
    [ProtoMember(11)] public List<CareNote> Notes { get; set; } = [];
    [ProtoMember(12)] public List<CareStep> History { get; set; } = [];
    [ProtoMember(13)] public string FirstName { get; set; } = "";
    [ProtoMember(14)] public bool MayRead { get; set; }
}

[ProtoContract]
public sealed class CareNote
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public string Author { get; set; } = "";
    [ProtoMember(4)] public DateTime At { get; set; }
}

[ProtoContract]
public sealed class CareStep
{
    [ProtoMember(1)] public string What { get; set; } = "";
    [ProtoMember(2)] public string Who { get; set; } = "";
    [ProtoMember(3)] public DateTime At { get; set; }
}

[ProtoContract]
public sealed class CareChange
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public CareState Care { get; set; }
    [ProtoMember(3)] public string AnsweredNote { get; set; } = "";
}

[ProtoContract]
public sealed class NoteSave
{
    [ProtoMember(1)] public string RequestId { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
}

[ProtoContract]
public sealed class NoteRef
{
    [ProtoMember(1)] public string RequestId { get; set; } = "";
    [ProtoMember(2)] public string NoteId { get; set; } = "";
}

[ProtoContract]
public sealed class WallSave
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public bool ShowName { get; set; }
    [ProtoMember(4)] public DateTime Until { get; set; }
}

[ProtoContract]
public sealed class WallCase
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public DateTime SentAt { get; set; }
    [ProtoMember(4)] public WallState Wall { get; set; }
    [ProtoMember(5)] public bool Minor { get; set; }
    [ProtoMember(6)] public string FirstName { get; set; } = "";
    [ProtoMember(7)] public string WallText { get; set; } = "";
    [ProtoMember(8)] public bool WallShowsName { get; set; }
    [ProtoMember(9)] public DateTime? WallUntil { get; set; }
    [ProtoMember(10)] public int Prayed { get; set; }
    [ProtoMember(11)] public DateTime ChangedAt { get; set; }
}

[ProtoContract]
public sealed class CareCase
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Text { get; set; } = "";
    [ProtoMember(3)] public string From { get; set; } = "";
    [ProtoMember(4)] public DateTime SentAt { get; set; }
    [ProtoMember(5)] public CareState Care { get; set; }
    [ProtoMember(6)] public WallState Wall { get; set; }
    [ProtoMember(7)] public bool Minor { get; set; }
    [ProtoMember(8)] public bool SignedIn { get; set; }
    [ProtoMember(9)] public bool WantsContact { get; set; }
    [ProtoMember(10)] public string Contact { get; set; } = "";
    [ProtoMember(11)] public string AnsweredNote { get; set; } = "";
    [ProtoMember(12)] public List<CareNote> Notes { get; set; } = [];
    [ProtoMember(13)] public DateTime ChangedAt { get; set; }
}
