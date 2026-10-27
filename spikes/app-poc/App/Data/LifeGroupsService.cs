using Gsbc.AppPoc.Native;

namespace Gsbc.AppPoc.Data;

public sealed record HomeLine(string Title, string Subtitle, string Href, string? Colour = null);

public sealed class LifeGroupsService(ChurchClock clock, NativeBridge native) : IMapLayer
{
    private const string JoinedKey = "lifegroups:joined";
    private const string RemindersKey = "lifegroups:reminders";
    private static readonly DateTime RoundPreviewMoment = new(2026, 10, 11, 8, 20, 0);

    private readonly HashSet<string> joined = [];
    private readonly HashSet<string> reminders = [];
    private Task? loading;

    public bool PreviewInRound { get; private set; }

    public event Action? Changed;

    public string Id => "lifegroups";

    public string Name => "Life Groups";

    public DateTime Now => PreviewInRound ? RoundPreviewMoment : clock.Now;

    public DateOnly Today => DateOnly.FromDateTime(Now);

    public LifeGroupRound? Current => LifeGroupsContent.Rounds.FirstOrDefault(r => r.First <= Today && Today <= r.Last);

    public LifeGroupRound? Next => LifeGroupsContent.Rounds.Where(r => r.First > Today).MinBy(r => r.First);

    public LifeGroupRound? Previous => LifeGroupsContent.Rounds.Where(r => r.Last < Today).MaxBy(r => r.Last);

    public LifeGroupRound? Showing => Current ?? Next;

    public IEnumerable<LifeGroup> All => LifeGroupsContent.AllYear.Concat(LifeGroupsContent.Rounds.SelectMany(r => r.Groups));

    public IReadOnlyList<LifeGroup> Yours =>
        All.Where(g => joined.Contains(g.Id) && (RoundOf(g) is not { } round || round == Showing)).ToList();

    public Task EnsureLoadedAsync() => loading ??= LoadAsync();

    public LifeGroup? Find(string id) => All.FirstOrDefault(g => g.Id == id);

    public LifeGroupRound? RoundOf(LifeGroup group) => LifeGroupsContent.Rounds.FirstOrDefault(r => r.Groups.Contains(group));

    public bool IsJoined(LifeGroup group) => joined.Contains(group.Id);

    public bool HasReminder(LifeGroup group) => reminders.Contains(group.Id);

    public bool IsFinished(LifeGroup group) => RoundOf(group) is { } round && round.Last < Today;

    public DateOnly NextSunday(LifeGroup group)
    {
        var after = Now.TimeOfDay > LifeGroupsContent.End.ToTimeSpan() ? Today.AddDays(1) : Today;
        var sunday = after.AddDays(((int)DayOfWeek.Sunday - (int)after.DayOfWeek + 7) % 7);
        return RoundOf(group) is { } round ? round.Sundays.FirstOrDefault(d => d >= sunday, round.Last) : sunday;
    }

    public SessionChange? ChangeOn(LifeGroup group, DateOnly date) =>
        LifeGroupsContent.Changes.FirstOrDefault(c => c.GroupId == group.Id && c.Date == date);

    public Place? PlaceOf(LifeGroup group) => group.PlaceId is { } id ? SitePlan.Find(id) : null;

    public Place? PlaceOn(LifeGroup group, DateOnly date) =>
        ChangeOn(group, date)?.MovedToPlaceId is { } moved ? SitePlan.Find(moved) : PlaceOf(group);

    public string RoomLabel(LifeGroup group) =>
        PlaceOf(group) is { } place ? $"Room {group.MapNumber} · {place.Name}" : "Room to be confirmed";

    public string WhenLabel(DateOnly date)
    {
        var days = date.DayNumber - Today.DayNumber;
        var day = days switch
        {
            0 => "Today",
            1 => "Tomorrow",
            < 7 => "This Sunday",
            _ => $"Sunday {date:d MMM}",
        };
        return $"{day}, {ChurchClock.Time(DateTime.Today + LifeGroupsContent.Start.ToTimeSpan())}";
    }

    public int NumberAt(LifeGroup group, Place place) =>
        LifeGroupsContent.AllYear.Concat(RoundOf(group)?.Groups ?? []).FirstOrDefault(g => g.PlaceId == place.Id)?.MapNumber ?? group.MapNumber;

    public MapMark MarkFor(LifeGroup group, Place place) =>
        new(place, group.Title, group.Colour, NumberAt(group, place).ToString(), $"Room {NumberAt(group, place)} · {place.Name}", $"groups/{group.Id}");

    public IReadOnlyList<MapMark> Marks() =>
        LifeGroupsContent.AllYear.Concat(Showing?.Groups ?? [])
            .Select(g => (Group: g, Place: PlaceOf(g)))
            .Where(x => x.Place is not null)
            .Select(x => MarkFor(x.Group, x.Place!))
            .ToList();

    public HomeLine Home()
    {
        if ((Yours.FirstOrDefault(g => RoundOf(g) is not null) ?? Yours.FirstOrDefault()) is { } group)
        {
            var sunday = NextSunday(group);
            var subtitle = ChangeOn(group, sunday) is { } change ? $"{WhenLabel(sunday)} · {change.Summary}" : $"{WhenLabel(sunday)} · {RoomLabel(group)}";
            return new(group.Title, subtitle, $"groups/{group.Id}", group.Colour);
        }
        if (Current is { } current)
        {
            return new("Life Groups", $"{current.Groups.Count} classes this round, Sundays 9am · choose one", "groups");
        }
        if (Next is { } next)
        {
            var what = next.Groups is [var only] ? only.Audience ?? only.Title : $"{next.Groups.Count} classes";
            return new($"{next.Name} starts {next.First:d MMM}", $"Life Groups · {what}, Sundays 9am", "groups");
        }
        return new("Life Groups", "Sundays 9am, before the morning service", "groups");
    }

    public async Task SetJoinedAsync(LifeGroup group, bool on)
    {
        Toggle(joined, group.Id, on);
        if (!on)
        {
            reminders.Remove(group.Id);
        }
        await SaveAsync();
    }

    public async Task SetReminderAsync(LifeGroup group, bool on)
    {
        Toggle(reminders, group.Id, on);
        await SaveAsync();
    }

    public void SetPreviewInRound(bool on)
    {
        PreviewInRound = on;
        Changed?.Invoke();
    }

    private async Task LoadAsync()
    {
        joined.UnionWith(Split(await native.GetAsync(JoinedKey)));
        reminders.UnionWith(Split(await native.GetAsync(RemindersKey)));
        Changed?.Invoke();
    }

    private async Task SaveAsync()
    {
        await native.SetAsync(JoinedKey, string.Join(',', joined));
        await native.SetAsync(RemindersKey, string.Join(',', reminders));
        Changed?.Invoke();
    }

    private static void Toggle(HashSet<string> set, string id, bool on)
    {
        if (on)
        {
            set.Add(id);
        }
        else
        {
            set.Remove(id);
        }
    }

    private static IEnumerable<string> Split(string? saved) => saved?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? [];
}
