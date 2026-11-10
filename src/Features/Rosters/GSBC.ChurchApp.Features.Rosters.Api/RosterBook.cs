using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed record Recipient(bool Leads, HashSet<Guid> People);

internal sealed class RosterBook(
    MinistryFacts ministry,
    SettingsRecord? settings,
    IReadOnlyList<PositionRecord> positions,
    IReadOnlyList<TrainedRecord> trained,
    IReadOnlyList<DateRecord> dates,
    IReadOnlyList<AssignmentRecord> assignments,
    IReadOnlyList<SwapRecord> swaps,
    IReadOnlyList<MinistryMember> team,
    IReadOnlyDictionary<Guid, FamilyFacts> families,
    IReadOnlyDictionary<Guid, string> names,
    IReadOnlyDictionary<Guid, SafeguardingStanding> standings,
    IReadOnlyList<AwayRecord> away,
    DateTime now)
{
    private readonly ILookup<Guid, AssignmentRecord> _byDate = assignments.ToLookup(assignment => assignment.DateId);
    private readonly ILookup<Guid, AwayDays> _away = away.ToLookup(span => span.PersonId, span => new AwayDays(span.From, span.To));
    private readonly HashSet<(Guid Position, Guid Person)> _trained = trained.Select(each => (each.PositionId, each.PersonId)).ToHashSet();
    private readonly Dictionary<Guid, PositionRecord> _positions = positions.ToDictionary(position => position.Id);

    public MinistryFacts Ministry => ministry;

    public RosterSettings Settings { get; } = RosterRules.SettingsOf(settings);

    public IReadOnlyList<MinistryMember> Team => team;

    public string NameOf(Guid? person) => person is { } id ? names.GetValueOrDefault(id) ?? "Someone" : RosterDefaults.FormerMember;

    public IReadOnlyDictionary<Guid, Recipient> Recipients()
    {
        Dictionary<Guid, Recipient> recipients = [];
        void Add(Guid person, bool leads, Guid about)
        {
            if (!recipients.TryGetValue(person, out Recipient? recipient))
                recipients[person] = recipient = new Recipient(leads, []);
            else if (leads && !recipient.Leads)
                recipients[person] = recipient = recipient with { Leads = true };
            recipient.People.Add(about);
        }

        foreach (MinistryMember member in team)
            Add(member.PersonId, member.Leads, member.PersonId);
        foreach (Guid person in assignments.Select(assignment => assignment.PersonId).OfType<Guid>().Distinct())
            Add(person, false, person);
        foreach (Guid person in recipients.Keys.ToList())
            if (families.GetValueOrDefault(person) is { Minor: true } facts)
                foreach (Guid parent in facts.Parents)
                    Add(parent, false, person);
        return recipients;
    }

    public RosterView ViewFor(Guid viewer, Recipient recipient)
    {
        bool leads = recipient.Leads;
        HashSet<string> yours = recipient.People.Select(person => person.ToString()).ToHashSet();
        return new RosterView
        {
            MinistryId = ministry.Id.ToString(),
            MinistryName = ministry.Name,
            Archived = ministry.Archived,
            MayEdit = leads && !ministry.Archived,
            Settings = Settings,
            Positions = positions.OrderBy(position => position.Order).ThenBy(position => position.Name).Select(position => PositionOf(position, leads)).ToList(),
            Dates = dates.OrderBy(date => date.StartsAt)
                .Select(date => RosterRules.Visible(DateOf(date, leads), Settings.WhoSees, leads, yours))
                .OfType<RosterDate>()
                .ToList(),
            Team = leads ? TeamLines() : [],
            TeamNames = leads || Settings.SwapWays.Contains(SwapWays.Direct)
                ? team.Select(member => new PersonLine { Id = member.PersonId.ToString(), Name = NameOf(member.PersonId) }).OrderBy(line => line.Name, StringComparer.CurrentCulture).ToList()
                : [],
            YourId = viewer.ToString(),
            You = recipient.People.OrderBy(person => person == viewer ? 0 : 1).ThenBy(person => NameOf(person))
                .Select(person => new PersonLine { Id = person.ToString(), Name = NameOf(person) }).ToList(),
        };
    }

    public RosterView Everything(Guid viewer) => ViewFor(viewer, new Recipient(true, []));

    private Position PositionOf(PositionRecord position, bool leads) => new()
    {
        Id = position.Id.ToString(), Name = position.Name, Needed = position.Needed, Order = position.Order,
        NeedsBlueCard = position.NeedsBlueCard, NeedsTraining = position.NeedsTraining, NeedsTrained = position.NeedsTrained,
        Trained = leads ? trained.Where(each => each.PositionId == position.Id).Select(each => each.PersonId.ToString()).ToList() : [],
    };

    private RosterDate DateOf(DateRecord date, bool leads)
    {
        DateOnly day = ChurchTime.DayOf(date.StartsAt);
        List<AssignmentRecord> on = _byDate[date.Id].OrderBy(assignment => assignment.AssignedAt).ToList();
        return new RosterDate
        {
            Id = date.Id.ToString(), EventKey = date.EventKey, Title = date.Title, CalendarName = date.CalendarName,
            StartsAt = date.StartsAt, EndsAt = date.EndsAt, AllDay = date.AllDay, NotOn = date.NotOn || date.Gone,
            Assignments = on.Select(assignment => AssignmentOf(assignment, day, on, leads)).ToList(),
        };
    }

    private Assignment AssignmentOf(AssignmentRecord assignment, DateOnly day, IReadOnlyList<AssignmentRecord> sameDate, bool leads)
    {
        string name = NameOf(assignment.PersonId);
        SwapRecord? swap = swaps.Where(each => each.AssignmentId == assignment.Id && SwapStates.Live(each.State)).MaxBy(each => each.AskedAt);
        return new Assignment
        {
            Id = assignment.Id.ToString(), PositionId = assignment.PositionId.ToString(), PositionName = assignment.PositionName,
            PersonId = assignment.PersonId?.ToString() ?? "", Name = name, State = assignment.State, Reason = assignment.Reason,
            RepliedBy = assignment.RepliedBy is { } by && by != assignment.PersonId ? RosterRules.ForWords(NameOf(by), name) : "",
            Flags = leads && assignment.PersonId is { } person && Replies.Fills(assignment.State) ? FlagsOf(assignment, person, day, sameDate) : [],
            Swap = swap is null ? null : new SwapLine
            {
                Id = swap.Id.ToString(), Way = swap.Way, State = swap.State, Note = swap.Note,
                AskedToId = swap.AskedTo?.ToString() ?? "", AskedToName = swap.AskedTo is null ? "" : NameOf(swap.AskedTo),
                OfferedById = swap.OfferedBy?.ToString() ?? "", OfferedByName = swap.OfferedBy is null ? "" : NameOf(swap.OfferedBy),
            },
        };
    }

    private List<string> FlagsOf(AssignmentRecord assignment, Guid person, DateOnly day, IReadOnlyList<AssignmentRecord> sameDate) =>
        _positions.TryGetValue(assignment.PositionId, out PositionRecord? position)
            ? RosterRules.Flags(position, day, _away[person], standings.GetValueOrDefault(person), _trained.Contains((position.Id, person)),
                sameDate.Where(other => other.Id != assignment.Id && other.PersonId == person && Replies.Fills(other.State)).Select(other => other.PositionName)).ToList()
            : [];

    private List<TeamMember> TeamLines() => team
        .Select(member => member.PersonId)
        .Concat(assignments.Where(assignment => assignment.PersonId is not null && dates.Any(date => date.Id == assignment.DateId && date.StartsAt > now))
            .Select(assignment => assignment.PersonId!.Value))
        .Distinct()
        .Select(person =>
        {
            FamilyFacts? family = families.GetValueOrDefault(person);
            SafeguardingStanding? standing = standings.GetValueOrDefault(person);
            return new TeamMember
            {
                PersonId = person.ToString(), Name = NameOf(person), Age = family is { Minor: true } ? family.Age : null,
                Leads = team.Any(member => member.PersonId == person && member.Leads),
                HoldsBlueCard = standing?.HoldsBlueCard == true, TrainingCurrent = standing?.TrainingCurrent == true,
                Away = _away[person].Where(span => span.To >= ChurchTime.DayOf(now))
                    .Select(span => new AwaySpan { From = Wire(span.From), To = Wire(span.To) }).ToList(),
                ReachedThrough = family is { Minor: true, CanSignIn: false, Parents.Count: > 0 }
                    ? string.Join(" and ", family.Parents.Select(parent => RosterRules.First(NameOf(parent))))
                    : "",
            };
        })
        .OrderBy(member => member.Name, StringComparer.CurrentCulture)
        .ToList();

    public static string Wire(DateOnly day) => day.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
}
