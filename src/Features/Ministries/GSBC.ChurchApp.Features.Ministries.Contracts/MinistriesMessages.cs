using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class DirectoryRequest;

[ProtoContract]
public sealed class MinistryDirectory
{
    [ProtoMember(1)] public List<MinistrySummary> Ministries { get; set; } = [];
    [ProtoMember(2)] public bool MayCreate { get; set; }
}

[ProtoContract]
public sealed class MinistrySummary
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string ParentId { get; set; } = "";
    [ProtoMember(3)] public string Kind { get; set; } = "";
    [ProtoMember(4)] public string Name { get; set; } = "";
    [ProtoMember(5)] public string Summary { get; set; } = "";
    [ProtoMember(6)] public string Meets { get; set; } = "";
    [ProtoMember(7)] public string Colour { get; set; } = "";
    [ProtoMember(8)] public string Icon { get; set; } = "";
    [ProtoMember(9)] public string TeamJoin { get; set; } = "";
    [ProtoMember(10)] public string AttendJoin { get; set; } = "";
    [ProtoMember(11)] public int Order { get; set; }
    [ProtoMember(12)] public string Yours { get; set; } = "";
    [ProtoMember(13)] public string RoundId { get; set; } = "";
    [ProtoMember(14)] public string Room { get; set; } = "";
    [ProtoMember(15)] public bool JoinAsParent { get; set; }
    [ProtoMember(16)] public string Latest { get; set; } = "";
}

[ProtoContract]
public sealed class MinistryRef : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class MinistryDetails
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string ParentId { get; set; } = "";
    [ProtoMember(3)] public string Kind { get; set; } = "";
    [ProtoMember(4)] public string Name { get; set; } = "";
    [ProtoMember(5)] public string Summary { get; set; } = "";
    [ProtoMember(6)] public string About { get; set; } = "";
    [ProtoMember(7)] public string Meets { get; set; } = "";
    [ProtoMember(8)] public string WhoFor { get; set; } = "";
    [ProtoMember(9)] public string Colour { get; set; } = "";
    [ProtoMember(10)] public string Icon { get; set; } = "";
    [ProtoMember(11)] public string TeamJoin { get; set; } = "";
    [ProtoMember(12)] public string AttendJoin { get; set; } = "";
    [ProtoMember(13)] public string RoundId { get; set; } = "";
    [ProtoMember(14)] public string Room { get; set; } = "";
    [ProtoMember(15)] public bool JoinAsParent { get; set; }
    [ProtoMember(16)] public int Order { get; set; }
}

[ProtoContract]
public sealed class MinistryPage
{
    [ProtoMember(1, IsRequired = true)] public MinistryDetails Ministry { get; set; } = new();
    [ProtoMember(2)] public List<PersonLine> Leaders { get; set; } = [];
    [ProtoMember(3)] public List<MinistrySummary> Teams { get; set; } = [];
    [ProtoMember(4)] public List<Crumb> Path { get; set; } = [];
    [ProtoMember(5)] public List<Post> Posts { get; set; } = [];
    [ProtoMember(6)] public List<FileItem> Files { get; set; } = [];
    [ProtoMember(7)] public List<CalendarLine> Calendars { get; set; } = [];
    [ProtoMember(8, IsRequired = true)] public YourPlace You { get; set; } = new();
    [ProtoMember(9)] public List<SessionChange> Sessions { get; set; } = [];
    [ProtoMember(10)] public Round? Round { get; set; }
}

[ProtoContract]
public sealed class Crumb
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class PersonLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class YourPlace
{
    [ProtoMember(1)] public bool SignedIn { get; set; }
    [ProtoMember(2)] public List<string> Roles { get; set; } = [];
    [ProtoMember(3)] public bool LeadsFromAbove { get; set; }
    [ProtoMember(4)] public List<ChildPlace> Children { get; set; } = [];
    [ProtoMember(5)] public List<AskLine> Asked { get; set; } = [];
    [ProtoMember(6)] public List<string> MayPostFor { get; set; } = [];
    [ProtoMember(7)] public bool MayManagePeople { get; set; }
    [ProtoMember(8)] public bool MayEditSettings { get; set; }
    [ProtoMember(9)] public bool MayDelete { get; set; }
    [ProtoMember(10)] public int Waiting { get; set; }
    [ProtoMember(11)] public bool ParentOfAttendee { get; set; }
}

[ProtoContract]
public sealed class ChildPlace
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public int? Age { get; set; }
    [ProtoMember(4)] public bool Attends { get; set; }
    [ProtoMember(5)] public bool OnTeam { get; set; }
    [ProtoMember(6)] public bool Asked { get; set; }
}

[ProtoContract]
public sealed class Post
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string MinistryId { get; set; } = "";
    [ProtoMember(3)] public string Title { get; set; } = "";
    [ProtoMember(4)] public string Body { get; set; } = "";
    [ProtoMember(5)] public string Audience { get; set; } = "";
    [ProtoMember(6)] public bool Important { get; set; }
    [ProtoMember(7)] public string Author { get; set; } = "";
    [ProtoMember(8)] public DateTime PostedAt { get; set; }
    [ProtoMember(9)] public DateTime? EditedAt { get; set; }
    [ProtoMember(10)] public string From { get; set; } = "";
    [ProtoMember(11)] public bool AskWholeChurch { get; set; }
    [ProtoMember(12)] public string WholeChurch { get; set; } = "";
}

[ProtoContract]
public sealed class FileItem
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string MinistryId { get; set; } = "";
    [ProtoMember(3)] public string Title { get; set; } = "";
    [ProtoMember(4)] public string Kind { get; set; } = "";
    [ProtoMember(5)] public string Url { get; set; } = "";
    [ProtoMember(6)] public string MediaId { get; set; } = "";
    [ProtoMember(7)] public string Audience { get; set; } = "";
    [ProtoMember(8)] public DateTime AddedAt { get; set; }
    [ProtoMember(9)] public string Note { get; set; } = "";
    [ProtoMember(10)] public string From { get; set; } = "";
}

[ProtoContract]
public sealed class CalendarLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Audience { get; set; } = "";
    [ProtoMember(4)] public string Slug { get; set; } = "";
}

[ProtoContract]
public sealed class JoinRequest : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string PersonId { get; set; } = "";
    [ProtoMember(3)] public string As { get; set; } = "";
    [ProtoMember(4)] public string Note { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class JoinOutcome
{
    [ProtoMember(1)] public bool Joined { get; set; }
    [ProtoMember(2)] public bool Asked { get; set; }
    [ProtoMember(3)] public string Words { get; set; } = "";
}

[ProtoContract]
public sealed class AskLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string PersonId { get; set; } = "";
    [ProtoMember(3)] public string Name { get; set; } = "";
    [ProtoMember(4)] public int? Age { get; set; }
    [ProtoMember(5)] public string As { get; set; } = "";
    [ProtoMember(6)] public string AskedBy { get; set; } = "";
    [ProtoMember(7)] public string Note { get; set; } = "";
    [ProtoMember(8)] public DateTime AskedAt { get; set; }
    [ProtoMember(9)] public string MinistryId { get; set; } = "";
    [ProtoMember(10)] public string MinistryName { get; set; } = "";
}

[ProtoContract]
public sealed class AskRef : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string AskId { get; set; } = "";
    [ProtoMember(3)] public bool Yes { get; set; }

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class PeopleList
{
    [ProtoMember(1)] public List<Member> Team { get; set; } = [];
    [ProtoMember(2)] public List<Member> Attendees { get; set; } = [];
    [ProtoMember(3)] public List<AskLine> Asks { get; set; } = [];
    [ProtoMember(4)] public List<string> MayGive { get; set; } = [];
    [ProtoMember(5)] public bool Attending { get; set; }
}

[ProtoContract]
public sealed class Member
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Role { get; set; } = "";
    [ProtoMember(4)] public int? Age { get; set; }
    [ProtoMember(5)] public List<PersonLine> Parents { get; set; } = [];
    [ProtoMember(6)] public bool NoParent { get; set; }
    [ProtoMember(7)] public bool CanSignIn { get; set; }
    [ProtoMember(8)] public string LeadsFrom { get; set; } = "";
}

[ProtoContract]
public sealed class PersonChange : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string PersonId { get; set; } = "";
    [ProtoMember(3)] public string Role { get; set; } = "";
    [ProtoMember(4)] public bool Add { get; set; }

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class FindRequest : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string Query { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class FoundPeople
{
    [ProtoMember(1)] public List<FoundPerson> People { get; set; } = [];
}

[ProtoContract]
public sealed class FoundPerson
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Detail { get; set; } = "";
}

[ProtoContract]
public sealed class LinkParentRequest : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string ChildId { get; set; } = "";
    [ProtoMember(3)] public string ParentId { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class PostSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public Post Post { get; set; } = new();

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class ItemRef : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string Id { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class FileSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public FileItem File { get; set; } = new();

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class MinistrySave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public MinistryDetails Details { get; set; } = new();

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class DeletionPreview
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public int Teams { get; set; }
    [ProtoMember(3)] public int People { get; set; }
    [ProtoMember(4)] public int Posts { get; set; }
    [ProtoMember(5)] public int Files { get; set; }
    [ProtoMember(6)] public List<string> Calendars { get; set; } = [];
    [ProtoMember(7)] public int Events { get; set; }
    [ProtoMember(8)] public string TeamsGoTo { get; set; } = "";
}

[ProtoContract]
public sealed class CalendarSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public CalendarLine Calendar { get; set; } = new();

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class CalendarRemoval
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public int Events { get; set; }
}

[ProtoContract]
public sealed class LifeGroupsRequest;

[ProtoContract]
public sealed class LifeGroupsPage
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public List<Round> Rounds { get; set; } = [];
    [ProtoMember(3)] public List<MinistrySummary> Groups { get; set; } = [];
    [ProtoMember(4)] public List<GroupSessions> Sessions { get; set; } = [];
    [ProtoMember(5)] public bool MaySetUp { get; set; }
}

[ProtoContract]
public sealed class Round
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public List<string> Sundays { get; set; } = [];
    [ProtoMember(4)] public string Starts { get; set; } = "";
    [ProtoMember(5)] public string Ends { get; set; } = "";
    [ProtoMember(6)] public string ShowsFrom { get; set; } = "";
}

[ProtoContract]
public sealed class GroupSessions
{
    [ProtoMember(1)] public string GroupId { get; set; } = "";
    [ProtoMember(2)] public List<SessionChange> Changes { get; set; } = [];
}

[ProtoContract]
public sealed class SessionChange
{
    [ProtoMember(1)] public string Date { get; set; } = "";
    [ProtoMember(2)] public bool Off { get; set; }
    [ProtoMember(3)] public string Note { get; set; } = "";
}

[ProtoContract]
public sealed class RoundSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public Round Round { get; set; } = new();

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class SessionSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public SessionChange Change { get; set; } = new();

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class WaitingRequest;

[ProtoContract]
public sealed class WaitingList
{
    [ProtoMember(1)] public List<AskLine> Asks { get; set; } = [];
}
