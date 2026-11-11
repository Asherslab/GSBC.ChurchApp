using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Rosters.Contracts;

[ProtoContract]
public sealed class Done;

[ProtoContract]
public sealed class Nothing;

[ProtoContract]
public sealed class RosterView
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string MinistryName { get; set; } = "";
    [ProtoMember(3)] public bool Archived { get; set; }
    [ProtoMember(4)] public bool MayEdit { get; set; }
    [ProtoMember(5, IsRequired = true)] public RosterSettings Settings { get; set; } = new();
    [ProtoMember(6)] public List<Position> Positions { get; set; } = [];
    [ProtoMember(7)] public List<RosterDate> Dates { get; set; } = [];
    [ProtoMember(8)] public List<TeamMember> Team { get; set; } = [];
    [ProtoMember(9)] public List<PersonLine> You { get; set; } = [];
    [ProtoMember(10)] public string YourId { get; set; } = "";
    [ProtoMember(11)] public List<PersonLine> TeamNames { get; set; } = [];
}

[ProtoContract]
public sealed class RosterSettings
{
    [ProtoMember(1, IsRequired = true)] public bool Replies { get; set; } = true;
    [ProtoMember(3, IsRequired = true)] public string WhoSees { get; set; } = Contracts.WhoSees.Everything;
    [ProtoMember(4, IsRequired = true)] public int RemindDaysBefore { get; set; } = RosterDefaults.RemindDaysBefore;
    [ProtoMember(5, IsRequired = true)] public int NudgeAfterDays { get; set; } = RosterDefaults.NudgeAfterDays;
    [ProtoMember(6, IsRequired = true)] public string OpenSwaps { get; set; } = Contracts.OpenSwaps.Team;
    [ProtoMember(7)] public bool DirectSwaps { get; set; }
    [ProtoMember(8)] public List<string> Calendars { get; set; } = [];

    public bool SwapsOn => OpenSwaps != Contracts.OpenSwaps.Off || DirectSwaps;

    public bool DrawsFrom(string calendarId, bool ministrysOwn) => Calendars.Count > 0 ? Calendars.Contains(calendarId) : ministrysOwn;
}

[ProtoContract]
public sealed class Position
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3, IsRequired = true)] public int Needed { get; set; } = 1;
    [ProtoMember(4)] public bool NeedsBlueCard { get; set; }
    [ProtoMember(5)] public bool NeedsTraining { get; set; }
    [ProtoMember(6)] public bool NeedsTrained { get; set; }
    [ProtoMember(7)] public List<string> Trained { get; set; } = [];
    [ProtoMember(8)] public int Order { get; set; }
    [ProtoMember(9)] public bool New { get; set; }
}

[ProtoContract]
public sealed class RosterDate
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string EventKey { get; set; } = "";
    [ProtoMember(3)] public string Title { get; set; } = "";
    [ProtoMember(4)] public string CalendarName { get; set; } = "";
    [ProtoMember(5)] public DateTime StartsAt { get; set; }
    [ProtoMember(6)] public DateTime EndsAt { get; set; }
    [ProtoMember(7)] public bool AllDay { get; set; }
    [ProtoMember(8)] public bool NotOn { get; set; }
    [ProtoMember(9)] public List<Assignment> Assignments { get; set; } = [];
}

[ProtoContract]
public sealed class Assignment
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string PositionId { get; set; } = "";
    [ProtoMember(3)] public string PositionName { get; set; } = "";
    [ProtoMember(4)] public string PersonId { get; set; } = "";
    [ProtoMember(5)] public string Name { get; set; } = "";
    [ProtoMember(6, IsRequired = true)] public string State { get; set; } = Replies.Asked;
    [ProtoMember(7)] public string Reason { get; set; } = "";
    [ProtoMember(8)] public string RepliedBy { get; set; } = "";
    [ProtoMember(9)] public List<string> Flags { get; set; } = [];
    [ProtoMember(10)] public SwapLine? Swap { get; set; }
    [ProtoMember(11)] public bool Training { get; set; }

    public bool Fills => Replies.Fills(State) && !Training;
}

[ProtoContract]
public sealed class SwapLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Way { get; set; } = "";
    [ProtoMember(3)] public string State { get; set; } = "";
    [ProtoMember(4)] public string AskedToId { get; set; } = "";
    [ProtoMember(5)] public string AskedToName { get; set; } = "";
    [ProtoMember(6)] public string OfferedById { get; set; } = "";
    [ProtoMember(7)] public string OfferedByName { get; set; } = "";
    [ProtoMember(8)] public string Note { get; set; } = "";
}

[ProtoContract]
public sealed class TeamMember
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public int? Age { get; set; }
    [ProtoMember(4)] public bool Leads { get; set; }
    [ProtoMember(5)] public bool HoldsBlueCard { get; set; }
    [ProtoMember(6)] public bool TrainingCurrent { get; set; }
    [ProtoMember(7)] public List<AwaySpan> Away { get; set; } = [];
    [ProtoMember(8)] public string ReachedThrough { get; set; } = "";
}

[ProtoContract]
public sealed class PersonLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class AwaySpan
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string From { get; set; } = "";
    [ProtoMember(3)] public string To { get; set; } = "";
    [ProtoMember(4)] public string Note { get; set; } = "";
}

[ProtoContract]
public sealed class AwayList
{
    [ProtoMember(1)] public List<AwayOf> People { get; set; } = [];
    [ProtoMember(2)] public string YourId { get; set; } = "";
}

[ProtoContract]
public sealed class PersonRef
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";
}

[ProtoContract]
public sealed class AwayOf
{
    [ProtoMember(1)] public string PersonId { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public List<AwaySpan> Spans { get; set; } = [];
}

[ProtoContract]
public sealed class RosterQuery : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string From { get; set; } = "";
    [ProtoMember(3)] public string To { get; set; } = "";
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class EventChoices
{
    [ProtoMember(1)] public List<EventChoice> Events { get; set; } = [];
}

[ProtoContract]
public sealed class EventChoice
{
    [ProtoMember(1)] public string Key { get; set; } = "";
    [ProtoMember(2)] public string Title { get; set; } = "";
    [ProtoMember(3)] public string CalendarName { get; set; } = "";
    [ProtoMember(4)] public DateTime StartsAt { get; set; }
    [ProtoMember(5)] public DateTime EndsAt { get; set; }
    [ProtoMember(6)] public bool AllDay { get; set; }
    [ProtoMember(7)] public bool Own { get; set; }
    [ProtoMember(8)] public string CalendarId { get; set; } = "";
}

[ProtoContract]
public sealed class PositionSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public Position Position { get; set; } = new();
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
public sealed class TrainedChange : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string PositionId { get; set; } = "";
    [ProtoMember(3)] public string PersonId { get; set; } = "";
    [ProtoMember(4)] public bool Trained { get; set; }
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class SettingsSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public RosterSettings Settings { get; set; } = new();
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class DateAdd : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string DateId { get; set; } = "";
    [ProtoMember(3)] public string EventKey { get; set; } = "";
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class AssignRequest : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string AssignmentId { get; set; } = "";
    [ProtoMember(3)] public string DateId { get; set; } = "";
    [ProtoMember(4)] public string PositionId { get; set; } = "";
    [ProtoMember(5)] public string PersonId { get; set; } = "";
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class ReplyRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string AssignmentId { get; set; } = "";
    [ProtoMember(3)] public bool Accept { get; set; }
    [ProtoMember(4)] public string Reason { get; set; } = "";
}

[ProtoContract]
public sealed class SwapAsk
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string SwapId { get; set; } = "";
    [ProtoMember(3)] public string AssignmentId { get; set; } = "";
    [ProtoMember(4)] public string Way { get; set; } = "";
    [ProtoMember(5)] public string ToPersonId { get; set; } = "";
    [ProtoMember(6)] public string Note { get; set; } = "";
}

[ProtoContract]
public sealed class SwapTake
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string SwapId { get; set; } = "";
    [ProtoMember(3)] public string PersonId { get; set; } = "";
    [ProtoMember(4, IsRequired = true)] public bool Yes { get; set; } = true;
}

[ProtoContract]
public sealed class SwapRef
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string SwapId { get; set; } = "";
}

[ProtoContract]
public sealed class SwapDecision : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string SwapId { get; set; } = "";
    [ProtoMember(3)] public bool Yes { get; set; }
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class AwaySave
{
    [ProtoMember(1)] public string AwayId { get; set; } = "";
    [ProtoMember(2)] public string PersonId { get; set; } = "";
    [ProtoMember(3)] public string From { get; set; } = "";
    [ProtoMember(4)] public string To { get; set; } = "";
    [ProtoMember(5)] public string Note { get; set; } = "";
}

[ProtoContract]
public sealed class AwayRef
{
    [ProtoMember(1)] public string AwayId { get; set; } = "";
}
