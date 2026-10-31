namespace Gsbc.AppPoc.Data.Ministries;

public enum MinistryKind { Ministry, LifeGroup }

public enum JoinMode { Open, Ask, InviteOnly }

public enum Audience { Everyone, Members }

public enum MinistryModule { Calendar, Announcements, Chats, Rosters, Files }

public enum ViewerRole { Visitor, Member, Leader, Staff }

public enum Relation { Visitor, Member, Leader }

public enum ViewingSplit { Marked, TeamTabs, TeamRoom }

public enum FileKind { Document, Link, Recording }

public enum RosterReply { Waiting, Accepted, Declined, SwapAsked }

public enum ComposeKind { Announcement, Event, Link }

public sealed record Ministry(
    string Id,
    string Name,
    string Summary,
    IReadOnlyList<string> About,
    string? Meets,
    IReadOnlyList<Person> Leaders,
    string Colour,
    string Icon,
    JoinMode Join,
    bool ServingTeam,
    IReadOnlyList<MinistryModule> Modules,
    string? ParentId = null,
    string? WhoFor = null,
    bool WithChildren = false,
    bool MembersStartChats = false,
    MinistryKind Kind = MinistryKind.Ministry,
    LifeGroupPart? LifeGroup = null);

public sealed record LifeGroupPart(string? PlaceId, int MapNumber, bool ParentsJoin = false);

public sealed class MinistrySettings(Ministry ministry)
{
    public HashSet<MinistryModule> Modules { get; } = [.. ministry.Modules];
    public JoinMode Join { get; set; } = ministry.Join;
    public bool ServingTeam { get; set; } = ministry.ServingTeam;
    public bool MembersStartChats { get; set; } = ministry.MembersStartChats;
}

public sealed record MinistryPost(string MinistryId, string Title, string Body, DateTime Posted, Audience Audience, string Author, bool Important = false, bool ToAllTeams = false);

public sealed record MinistryEvent(string MinistryId, string Title, DateTime Start, TimeSpan Length, string Place, Audience Audience, string? Detail = null, bool Changed = false);

public sealed record MinistryFile(string MinistryId, string Title, string Detail, FileKind Kind, Audience Audience, string? Href = null, string? AudioPath = null);

public sealed record ChatMessage(string Author, string Text, DateTime At);

public sealed class MinistryChat(string id, string ministryId, string name, bool leadersOnly, IEnumerable<ChatMessage> messages)
{
    public string Id { get; } = id;
    public string MinistryId { get; } = ministryId;
    public string Name { get; } = name;
    public bool LeadersOnly { get; } = leadersOnly;
    public List<ChatMessage> Messages { get; } = [.. messages];
    public ChatMessage? Last => Messages.Count > 0 ? Messages[^1] : null;
}

public sealed record Song(string Title, string Detail, IReadOnlyList<MinistryFile> Practice);

public sealed class RosterSlot(string position, string ministryId, IEnumerable<Person> people, RosterReply reply = RosterReply.Waiting)
{
    public string Position { get; } = position;
    public string MinistryId { get; } = ministryId;
    public List<Person> People { get; } = [.. people];
    public RosterReply YourReply { get; set; } = reply;
}

public sealed record RosterDate(string MinistryId, DateTime Start, string Service, IReadOnlyList<RosterSlot> Slots, IReadOnlyList<Song> Songs);

public sealed record SlotOnRoster(RosterDate Date, RosterSlot Slot, Ministry Ministry);
