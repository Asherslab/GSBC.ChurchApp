using ProtoBuf;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class InboxItem
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Kind { get; set; } = "";
    [ProtoMember(3)] public string Area { get; set; } = "";
    [ProtoMember(4)] public string Title { get; set; } = "";
    [ProtoMember(5)] public string Body { get; set; } = "";
    [ProtoMember(6)] public string? Link { get; set; }
    [ProtoMember(7)] public string? Subject { get; set; }
    [ProtoMember(8)] public bool Urgent { get; set; }
    [ProtoMember(9)] public DateTime RaisedAt { get; set; }
    [ProtoMember(10)] public DateTime? ReadAt { get; set; }
    [ProtoMember(11)] public string? ScopeId { get; set; }
    [ProtoMember(12)] public bool Test { get; set; }
    [ProtoMember(13)] public bool Waiting { get; set; }
    [ProtoMember(14)] public string? Action { get; set; }
    [ProtoMember(15)] public bool InAdminPanel { get; set; }
    [ProtoMember(16)] public string? OneOf { get; set; }
    [ProtoMember(17)] public int Rank { get; set; }
}

[ProtoContract]
public sealed class KindInfo
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public string Description { get; set; } = "";
    [ProtoMember(3)] public string Area { get; set; } = "";
    [ProtoMember(4)] public bool DefaultPush { get; set; }
    [ProtoMember(5)] public bool Mandatory { get; set; }
    [ProtoMember(6)] public string? PerScope { get; set; }
    [ProtoMember(7)] public string? ForNode { get; set; }
    [ProtoMember(8)] public string? ForScope { get; set; }
    [ProtoMember(9)] public List<KindPermission> ForAny { get; set; } = [];
}

[ProtoContract]
public sealed class KindPermission
{
    [ProtoMember(1)] public string Node { get; set; } = "";
    [ProtoMember(2)] public string Scope { get; set; } = "";
}

[ProtoContract]
public sealed class PreferenceItem
{
    [ProtoMember(1)] public string Kind { get; set; } = "";
    [ProtoMember(2)] public string? ScopeId { get; set; }
    [ProtoMember(3)] public bool Push { get; set; }
}

[ProtoContract]
public sealed class MarkReadRequest
{
    [ProtoMember(1)] public List<string> Ids { get; set; } = [];
    [ProtoMember(2)] public bool All { get; set; }
}

[ProtoContract]
public sealed class RegisterDeviceRequest
{
    [ProtoMember(1)] public string Token { get; set; } = "";
    [ProtoMember(2)] public string Platform { get; set; } = "";
    [ProtoMember(3)] public string AppId { get; set; } = "";
    [ProtoMember(4)] public string Environment { get; set; } = "";
}

[ProtoContract]
public sealed class AudienceMessage
{
    [ProtoMember(1)] public string Kind { get; set; } = "";
    [ProtoMember(2)] public string? Node { get; set; }
    [ProtoMember(3)] public string? ObjectId { get; set; }
    [ProtoMember(4)] public string? UserId { get; set; }
}

[ProtoContract]
public sealed class Announcement
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public string Body { get; set; } = "";
    [ProtoMember(4)] public string? Link { get; set; }
    [ProtoMember(5)] public AudienceMessage? Audience { get; set; }
    [ProtoMember(6)] public bool Urgent { get; set; }
    [ProtoMember(7)] public string State { get; set; } = "";
    [ProtoMember(8)] public DateTime? SendAt { get; set; }
    [ProtoMember(9)] public DateTime? SentAt { get; set; }
    [ProtoMember(10)] public string? AuthorId { get; set; }
    [ProtoMember(11)] public DateTime UpdatedAt { get; set; }
    [ProtoMember(12)] public int ReachedPeople { get; set; }
    [ProtoMember(13)] public int ReachedPhones { get; set; }
    [ProtoMember(14)] public int ReadBy { get; set; }
    [ProtoMember(15)] public DateTime? RecalledAt { get; set; }
    [ProtoMember(16)] public string? RecalledBy { get; set; }
}

[ProtoContract]
public sealed class AnnouncementQuery;

[ProtoContract]
public sealed class AnnouncementList
{
    [ProtoMember(1)] public List<Announcement> Announcements { get; set; } = [];
}

[ProtoContract]
public sealed class AnnouncementRequest
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public DateTime? SendAt { get; set; }
}

[ProtoContract]
public sealed class Reach
{
    [ProtoMember(1)] public int People { get; set; }
    [ProtoMember(2)] public int Phones { get; set; }
}
