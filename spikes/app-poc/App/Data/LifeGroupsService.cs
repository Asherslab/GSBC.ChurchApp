using Gsbc.AppPoc.Data.Ministries;
using Gsbc.AppPoc.Native;

namespace Gsbc.AppPoc.Data;

public sealed record HomeLine(string Title, string Subtitle, string Href, string? Colour = null, string Icon = "users");

public sealed class LifeGroupsService : IMapLayer
{
    private const string EarlierJoinedKey = "lifegroups:joined";
    private const string RemindersKey = "lifegroups:reminders";
    private static readonly DateTime RoundPreviewMoment = new(2026, 10, 11, 8, 20, 0);

    private readonly HashSet<string> reminders = [];
    private readonly ChurchClock clock;
    private readonly NativeBridge native;
    private readonly MinistriesService ministries;
    private Task? loading;

    public LifeGroupsService(ChurchClock clock, NativeBridge native, MinistriesService ministries)
    {
        this.clock = clock;
        this.native = native;
        this.ministries = ministries;
        ministries.Changed += () => Changed?.Invoke();
    }

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

    public static IEnumerable<Ministry> All => MinistriesService.OfKind(MinistryKind.LifeGroup);

    public IReadOnlyList<Ministry> Yours =>
        All.Where(g => IsJoined(g) && (RoundOf(g) is not { } round || round == Showing)).ToList();

    public Task EnsureLoadedAsync() => loading ??= LoadAsync();

    public static Ministry? Find(string id) => All.FirstOrDefault(g => g.Id == id);

    public static LifeGroupRound? RoundOf(Ministry group) => LifeGroupsContent.RoundOf(group);

    public bool IsJoined(Ministry group) => ministries.IsOwnMember(group);

    public bool HasReminder(Ministry group) => reminders.Contains(group.Id);

    public bool IsFinished(Ministry group) => RoundOf(group) is { } round && round.Last < Today;

    public DateOnly NextSunday(Ministry group)
    {
        var after = Now.TimeOfDay > LifeGroupsContent.End.ToTimeSpan() ? Today.AddDays(1) : Today;
        var sunday = after.AddDays(((int)DayOfWeek.Sunday - (int)after.DayOfWeek + 7) % 7);
        return RoundOf(group) is { } round ? round.Sundays.FirstOrDefault(d => d >= sunday, round.Last) : sunday;
    }

    public SessionChange? ChangeOn(Ministry group, DateOnly date) =>
        LifeGroupsContent.Changes.FirstOrDefault(c => c.GroupId == group.Id && c.Date == date);

    public static Place? PlaceOf(Ministry group) => group.LifeGroup?.PlaceId is { } id ? SitePlan.Find(id) : null;

    public static int MapNumber(Ministry group) => group.LifeGroup?.MapNumber ?? 0;

    public Place? PlaceOn(Ministry group, DateOnly date) =>
        ChangeOn(group, date)?.MovedToPlaceId is { } moved ? SitePlan.Find(moved) : PlaceOf(group);

    public string RoomLabel(Ministry group) =>
        PlaceOf(group) is { } place ? $"Room {MapNumber(group)} · {place.Name}" : "Room to be confirmed";

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

    public int NumberAt(Ministry group, Place place) =>
        LifeGroupsContent.AllYear.Concat(RoundOf(group)?.Groups ?? []).FirstOrDefault(g => g.LifeGroup?.PlaceId == place.Id) is { } there ? MapNumber(there) : MapNumber(group);

    public MapMark MarkFor(Ministry group, Place place) =>
        new(place, group.Name, group.Colour, NumberAt(group, place).ToString(), $"Room {NumberAt(group, place)} · {place.Name}", $"groups/{group.Id}");

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
            return new(group.Name, subtitle, $"groups/{group.Id}", group.Colour);
        }
        if (Current is { } current)
        {
            return new("Life Groups", $"{current.Groups.Count} classes this round, Sundays 9am · choose one", "groups");
        }
        if (Next is { } next)
        {
            var what = next.Groups is [var only] ? only.WhoFor ?? only.Name : $"{next.Groups.Count} classes";
            return new($"{next.Name} starts {next.First:d MMM}", $"Life Groups · {what}, Sundays 9am", "groups");
        }
        return new("Life Groups", "Sundays 9am, before the morning service", "groups");
    }

    public async Task SetJoinedAsync(Ministry group, bool on)
    {
        if (on)
        {
            await ministries.JoinAsync(group);
            return;
        }
        reminders.Remove(group.Id);
        await SaveAsync();
        await ministries.LeaveAsync(group);
    }

    public async Task SetReminderAsync(Ministry group, bool on)
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
        await ministries.EnsureLoadedAsync();
        foreach (var earlier in Split(await native.GetAsync(EarlierJoinedKey)).Select(Find).OfType<Ministry>())
        {
            await ministries.JoinAsync(earlier);
        }
        await native.SetAsync(EarlierJoinedKey, "");
        reminders.UnionWith(Split(await native.GetAsync(RemindersKey)));
        Changed?.Invoke();
    }

    private async Task SaveAsync()
    {
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
