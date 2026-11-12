using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Ministries.Contracts;

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class DirectoryRequest;

[ProtoContract]
public sealed class ArchivedList
{
    [ProtoMember(1)] public List<ArchivedLine> Ministries { get; set; } = [];
}

[ProtoContract]
public sealed class ArchivedLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Where { get; set; } = "";
    [ProtoMember(4)] public DateTime ArchivedAt { get; set; }
    [ProtoMember(5)] public bool ShowsInLifeGroups { get; set; }
    [ProtoMember(6)] public bool MayRestore { get; set; }
}

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
    [ProtoMember(19)] public bool ShowsInLifeGroups { get; set; }
    [ProtoMember(16)] public string Latest { get; set; } = "";
    [ProtoMember(17)] public int Waiting { get; set; }
    [ProtoMember(18)] public bool MayAddUnder { get; set; }
    [ProtoMember(20)] public bool Hidden { get; set; }
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
    [ProtoMember(20)] public bool ShowsInLifeGroups { get; set; }
    [ProtoMember(16)] public int Order { get; set; }
    [ProtoMember(17)] public List<string> PartsOff { get; set; } = [];
    [ProtoMember(19)] public List<string> PartsFromBelow { get; set; } = [];
    [ProtoMember(21)] public bool New { get; set; }
    [ProtoMember(22)] public List<string> PartsOrder { get; set; } = [];
    [ProtoMember(23)] public string ChatStarters { get; set; } = "";
    [ProtoMember(24)] public bool Hidden { get; set; }
    [ProtoMember(25)] public string HiddenBy { get; set; } = "";
}

[ProtoContract]
public sealed class MinistryPage
{
    [ProtoMember(1, IsRequired = true)] public MinistryDetails Ministry { get; set; } = new();
    [ProtoMember(2)] public List<PersonLine> Leaders { get; set; } = [];
    [ProtoMember(3)] public List<MinistrySummary> SubMinistries { get; set; } = [];
    [ProtoMember(4)] public List<Crumb> Path { get; set; } = [];
    [ProtoMember(5)] public List<Post> Posts { get; set; } = [];
    [ProtoMember(6)] public List<FileItem> Files { get; set; } = [];
    [ProtoMember(7)] public List<CalendarLine> Calendars { get; set; } = [];
    [ProtoMember(8, IsRequired = true)] public YourPlace You { get; set; } = new();
    [ProtoMember(10)] public Round? Round { get; set; }
    [ProtoMember(11)] public List<SittingOut> SittingOut { get; set; } = [];
    [ProtoMember(12)] public List<string> Sundays { get; set; } = [];
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
    [ProtoMember(12)] public List<AskLine> ForYourYes { get; set; } = [];
    [ProtoMember(13)] public bool Minor { get; set; }
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
    [ProtoMember(13)] public bool New { get; set; }
    [ProtoMember(14)] public string Sunday { get; set; } = "";
    [ProtoMember(15)] public bool NoClass { get; set; }
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
    [ProtoMember(11)] public bool New { get; set; }
}

[ProtoContract]
public sealed class CalendarLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string Audience { get; set; } = "";
    [ProtoMember(4)] public string Slug { get; set; } = "";
    [ProtoMember(5)] public string MinistryId { get; set; } = "";
    [ProtoMember(6)] public string From { get; set; } = "";
    [ProtoMember(7)] public bool Linked { get; set; }
    [ProtoMember(10)] public List<DaySpan> Within { get; set; } = [];
    [ProtoMember(11)] public bool OnlyWithin { get; set; }
}

[ProtoContract]
public sealed class DaySpan
{
    [ProtoMember(1)] public string From { get; set; } = "";
    [ProtoMember(2)] public string To { get; set; } = "";
}

[ProtoContract]
public sealed class CalendarLinkSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string CalendarId { get; set; } = "";
    [ProtoMember(3)] public bool Linked { get; set; }

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class LinkableCalendars
{
    [ProtoMember(1)] public List<Crumb> Calendars { get; set; } = [];
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
    [ProtoMember(11)] public string AskedById { get; set; } = "";
    [ProtoMember(12)] public bool WaitingForParent { get; set; }
    [ProtoMember(13)] public bool NoParent { get; set; }
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
public sealed class PartsSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public List<string> PartsOff { get; set; } = [];
    [ProtoMember(4)] public List<string> PartsFromBelow { get; set; } = [];
    [ProtoMember(5)] public List<string> PartsOrder { get; set; } = [];
    [ProtoMember(6)] public string ChatStarters { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class DeletionPreview
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public int SubMinistries { get; set; }
    [ProtoMember(3)] public int People { get; set; }
    [ProtoMember(4)] public int Posts { get; set; }
    [ProtoMember(5)] public int Files { get; set; }
    [ProtoMember(6)] public List<string> Calendars { get; set; } = [];
    [ProtoMember(7)] public int Events { get; set; }
    [ProtoMember(8)] public string SubMinistriesGoTo { get; set; } = "";
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
    [ProtoMember(2)] public List<Round> Rounds { get; set; } = [];
    [ProtoMember(3)] public List<MinistrySummary> Groups { get; set; } = [];
    [ProtoMember(5)] public bool MayPlan { get; set; }
    [ProtoMember(6)] public List<Crumb> Under { get; set; } = [];
    [ProtoMember(7)] public string CalendarName { get; set; } = "";
    [ProtoMember(8)] public List<SittingOut> SittingOut { get; set; } = [];
}

[ProtoContract]
public sealed class SittingOut
{
    [ProtoMember(1)] public string GroupId { get; set; } = "";
    [ProtoMember(2)] public string RoundId { get; set; } = "";
    [ProtoMember(3)] public string Note { get; set; } = "";
    [ProtoMember(4)] public string InsteadId { get; set; } = "";
    [ProtoMember(5)] public string InsteadName { get; set; } = "";
    [ProtoMember(6)] public Round? Round { get; set; }
}

[ProtoContract]
public sealed class SitOutSave
{
    [ProtoMember(1, IsRequired = true)] public SittingOut SittingOut { get; set; } = new();
    [ProtoMember(2)] public bool Off { get; set; }
}

[ProtoContract]
public sealed class LifeGroupsCalendar
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
    [ProtoMember(2)] public string CalendarName { get; set; } = "";
    [ProtoMember(3)] public List<Crumb> Choices { get; set; } = [];
}

[ProtoContract]
public sealed class LifeGroupsCalendarChoice
{
    [ProtoMember(1)] public string CalendarId { get; set; } = "";
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
public sealed class RoundSave
{
    [ProtoMember(2, IsRequired = true)] public Round Round { get; set; } = new();
}

[ProtoContract]
public sealed class RoundRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class WaitingRequest;

[ProtoContract]
public sealed class WaitingList
{
    [ProtoMember(1)] public List<AskLine> Asks { get; set; } = [];
}

[ProtoContract]
public sealed class MoveRequest : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string UnderId { get; set; } = "";

    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class MovePreview
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public string From { get; set; } = "";
    [ProtoMember(3)] public string To { get; set; } = "";
    [ProtoMember(4)] public int SubMinistries { get; set; }
    [ProtoMember(5)] public List<LeadChange> Gain { get; set; } = [];
    [ProtoMember(6)] public List<LeadChange> Lose { get; set; } = [];
    [ProtoMember(7)] public List<string> Leaves { get; set; } = [];
    [ProtoMember(8)] public List<string> Joins { get; set; } = [];
    [ProtoMember(9)] public string HiddenBy { get; set; } = "";
    [ProtoMember(10)] public bool ShownAgain { get; set; }
}

[ProtoContract]
public sealed class LeadChange
{
    [ProtoMember(1)] public string Ministry { get; set; } = "";
    [ProtoMember(2)] public List<string> People { get; set; } = [];
}

[ProtoContract]
public sealed class MinistryCard
{
    [ProtoMember(1, IsRequired = true)] public MinistryDetails Ministry { get; set; } = new();
    [ProtoMember(2)] public List<PersonLine> Leaders { get; set; } = [];
}

[ProtoContract]
public sealed class MinistryPeople
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public PeopleList People { get; set; } = new();
}

[ProtoContract]
public sealed class YourMinistries
{
    [ProtoMember(1)] public List<ChildFacts> Children { get; set; } = [];
    [ProtoMember(2)] public List<YourRoles> Places { get; set; } = [];
    [ProtoMember(3)] public List<AskLine> Asked { get; set; } = [];
    [ProtoMember(4)] public string PersonId { get; set; } = "";
    [ProtoMember(5)] public bool Minor { get; set; }
}

[ProtoContract]
public sealed class ChildFacts
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public int? Age { get; set; }
}

[ProtoContract]
public sealed class YourRoles
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public List<string> Roles { get; set; } = [];
    [ProtoMember(3)] public List<string> ChildrenAttending { get; set; } = [];
    [ProtoMember(4)] public List<string> ChildrenOnTeam { get; set; } = [];
}
