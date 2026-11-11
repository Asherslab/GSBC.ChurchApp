using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryViews(
    MinistriesDbContext db,
    IRoleAssignments roles,
    IAccessEvaluator access,
    IFamilies families,
    IPeopleIndex people,
    IMinistryCalendars calendars)
{
    public async Task<MinistryTree> TreeAsync(CancellationToken cancellationToken) =>
        new(MinistryArchive.Active(await db.Ministries.AsNoTracking().ToListAsync(cancellationToken)));

    public async Task<MinistryTree> EveryTreeAsync(CancellationToken cancellationToken) =>
        new(await db.Ministries.AsNoTracking().ToListAsync(cancellationToken));

    public async Task<GrantSet> GrantsAsync(Guid? userId, CancellationToken cancellationToken) =>
        userId is { } person ? await access.GrantsForAsync(person, cancellationToken) : GrantSet.Empty;

    public static bool Holds(GrantSet grants, Permission permission, Guid ministryId) => grants.IsAllowed(permission, ministryId.ToString());

    public static bool MaySee(string audience, GrantSet grants, MinistryTree tree, Guid ministryId) =>
        MinistryLogic.MaySee(
            audience,
            node => grants.IsAllowed(Permission(node), ministryId.ToString()),
            node => tree.Below(ministryId).Any(below => grants.IsAllowed(Permission(node), below.Id.ToString())));

    public async Task<IReadOnlyList<string>> RolesAsync(Guid userId, Guid ministryId, CancellationToken cancellationToken)
    {
        List<string> held = [];
        foreach (string role in new[] { Roles.Leader, Roles.Admin, Roles.Team, Roles.Attendee })
            if ((await roles.HoldersAsync(Roles.KeyOf(role), ministryId.ToString(), cancellationToken)).Any(holder => holder.UserId == userId))
                held.Add(role);
        return held;
    }

    public async Task<MinistryHoldings> HoldingsAsync(IReadOnlyCollection<MinistryRecord> places, CancellationToken cancellationToken)
    {
        List<string> ids = places.Select(place => place.Id.ToString()).ToList();
        List<(string, Guid, Guid)> all = [];
        foreach (string role in MinistryHoldings.AllRoles)
            all.AddRange((await roles.HoldingsAsync(Roles.KeyOf(role), ids, cancellationToken))
                .Select(holding => (role, holding.UserId, Guid.Parse(holding.ObjectId))));
        return MinistryHoldings.From(all);
    }

    public async Task<IReadOnlyList<Guid>> HoldersAsync(string role, Guid ministryId, CancellationToken cancellationToken) =>
        (await roles.HoldersAsync(Roles.KeyOf(role), ministryId.ToString(), cancellationToken)).Select(holder => holder.UserId).Distinct().ToList();

    public async Task<List<PersonLine>> LeadersAsync(MinistryTree tree, Guid ministryId, CancellationToken cancellationToken)
    {
        foreach (Guid place in new[] { ministryId }.Concat(tree.Above(ministryId).Select(above => above.Id)))
        {
            IReadOnlyList<Guid> leaders = await HoldersAsync(Roles.Leader, place, cancellationToken);
            if (leaders.Count > 0)
                return await LinesAsync(leaders, cancellationToken);
        }
        return [];
    }

    private async Task<List<PersonLine>> LinesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(ids, cancellationToken);
        return ids.Select(id => new PersonLine { Id = id.ToString(), Name = names.GetValueOrDefault(id) ?? "Someone" })
            .OrderBy(line => line.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    public static bool IsOn(MinistryRecord ministry, string part) => MinistryParts.IsOn(ministry.PartsOff, part);

    public static List<Guid> Showing(MinistryTree tree, Guid ministryId, string part) =>
        IsOn(tree.Find(ministryId)!, part)
            ? [ministryId, .. tree.Above(ministryId).Where(place => IsOn(place, part)).Select(place => place.Id)]
            : [];

    public async Task<List<Post>> PostsAsync(MinistryTree tree, Guid ministryId, GrantSet grants, CancellationToken cancellationToken)
    {
        List<Guid> above = Showing(tree, ministryId, MinistryParts.News);
        List<Guid> below = [.. Gathered(tree, ministryId, MinistryParts.News).Where(id => id != ministryId)];
        List<Guid> ids = [.. above, .. below];
        List<PostRecord> records = await db.Posts.AsNoTracking().Where(post => ids.Contains(post.MinistryId))
            .OrderByDescending(post => post.PostedAt).Take(200).ToListAsync(cancellationToken);
        return records
            .Where(post => post.MinistryId == ministryId || below.Contains(post.MinistryId) || post.Audience != Audiences.Everyone)
            .Where(post => MaySee(post.Audience, grants, tree, post.MinistryId))
            .Select(post => Post(post, post.MinistryId == ministryId ? "" : tree.Find(post.MinistryId)?.Name ?? ""))
            .ToList();
    }

    public async Task<List<FileItem>> FilesAsync(MinistryTree tree, Guid ministryId, GrantSet grants, CancellationToken cancellationToken)
    {
        List<Guid> ids = Showing(tree, ministryId, MinistryParts.Files);
        List<FileRecord> records = await db.Files.AsNoTracking().Where(file => ids.Contains(file.MinistryId))
            .OrderBy(file => file.Title).ToListAsync(cancellationToken);
        return records
            .Where(file => file.MinistryId == ministryId || file.Audience != Audiences.Everyone)
            .Where(file => MaySee(file.Audience, grants, tree, file.MinistryId))
            .Select(file => File(file, file.MinistryId == ministryId ? "" : tree.Find(file.MinistryId)?.Name ?? ""))
            .ToList();
    }

    public async Task<List<CalendarLine>> CalendarsAsync(MinistryTree tree, Guid ministryId, GrantSet grants, bool signedIn, CancellationToken cancellationToken)
    {
        List<Guid> shown = [.. CalendarsShown(tree, ministryId)];
        if (shown.Count == 0)
            return [];
        Viewer viewer = Viewer.Of(grants, signedIn);
        return (await calendars.OfAsync(shown, cancellationToken))
            .Where(calendar => !calendar.Hidden && calendar.Audience.Includes(viewer))
            .OrderBy(calendar => shown.IndexOf(calendar.MinistryId))
            .Select(calendar => Calendar(calendar, calendar.MinistryId == ministryId ? "" : tree.Find(calendar.MinistryId)?.Name ?? ""))
            .ToList();
    }

    public static IReadOnlyList<Guid> CalendarsShown(MinistryTree tree, Guid ministryId) => Gathered(tree, ministryId, MinistryParts.Calendar);

    public static IReadOnlyList<Guid> Gathered(MinistryTree tree, Guid ministryId, string part)
    {
        MinistryRecord ministry = tree.Find(ministryId)!;
        if (!IsOn(ministry, part))
            return [];
        if (!ministry.PartsFromBelow.Contains(part))
            return [ministryId];
        return [ministryId, .. tree.Below(ministryId).Where(below => IsOn(below, part) && Reaches(tree, below, ministryId, part)).Select(below => below.Id)];
    }

    private static bool Reaches(MinistryTree tree, MinistryRecord below, Guid top, string part) =>
        tree.Above(below.Id).TakeWhile(above => above.Id != top).All(above => IsOn(above, part));

    public async Task<IReadOnlyDictionary<Guid, FamilyFacts>> FactsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await families.FactsAsync(ids, cancellationToken);

    public static CalendarLine Calendar(MinistryCalendar calendar, string from = "") => new()
    {
        Id = calendar.Id.ToString(), Name = calendar.Name, Slug = calendar.Slug, Audience = AudienceOf(calendar.Audience),
        MinistryId = calendar.MinistryId.ToString(), From = from,
    };

    public static CalendarLine Linked(MinistryCalendar calendar, Guid ministryId, IReadOnlyList<string>? days) => new()
    {
        Id = calendar.Id.ToString(), Name = calendar.Name, Slug = calendar.Slug, Audience = Audiences.Everyone, MinistryId = ministryId.ToString(), Linked = true,
        Days = [.. days ?? []], OnItsDays = days is not null,
    };

    public async Task<List<CalendarLine>> LinkedAsync(MinistryRecord ministry, IReadOnlyList<string>? days, CancellationToken cancellationToken) =>
        !IsOn(ministry, MinistryParts.Calendar) || ministry.LinkedCalendars.Count == 0
            ? []
            : (await calendars.ForEveryoneAsync(cancellationToken)).Where(calendar => ministry.LinkedCalendars.Contains(calendar.Id))
                .OrderBy(calendar => calendar.Name, StringComparer.Ordinal)
                .Select(calendar => Linked(calendar, ministry.Id, days))
                .ToList();

    public static List<string> SundaysOf(MinistryRecord group, IEnumerable<RoundRecord> rounds, IEnumerable<SitOutRecord> sitOuts) =>
        LifeGroupRounds.SundaysOf(group.Id.ToString(), group.RoundId?.ToString() ?? "", rounds.Select(Round),
            sitOuts.Where(sitOut => sitOut.GroupId == group.Id).Select(sitOut => new Contracts.SittingOut { GroupId = sitOut.GroupId.ToString(), RoundId = sitOut.RoundId.ToString() }));

    public static Audience WhileHidden(Guid ministryId) =>
        Audience.HoldingAt(MinistriesPermissions.SettingsNode, Scope.Of(ScopeKind.Ministry, ministryId.ToString()));

    private static string AudienceOf(Audience audience) => audience.Node switch
    {
        MinistriesPermissions.TeamViewNode => Audiences.Team,
        MinistriesPermissions.ParentsViewNode => Audiences.Parents,
        MinistriesPermissions.AttendeesViewNode => Audiences.Attendees,
        _ => Audiences.Everyone,
    };

    public static Audience CalendarAudience(string audience, Guid ministryId) =>
        Audiences.NodeOf(audience) is { } node ? Audience.HoldingAt(node, Scope.Of(ScopeKind.Ministry, ministryId.ToString())) : Audience.Everyone;

    private static Permission Permission(string node) =>
        MinistriesPermissions.All.FirstOrDefault(permission => permission.Node == node) ?? new Permission(node, ScopeKind.Ministry, node);

    public static MinistryDetails Details(MinistryRecord ministry) => new()
    {
        Id = ministry.Id.ToString(), ParentId = ministry.ParentId?.ToString() ?? "", Name = ministry.Name,
        Summary = ministry.Summary, About = ministry.About, Meets = ministry.Meets, WhoFor = ministry.WhoFor, Colour = ministry.Colour,
        Icon = ministry.Icon, TeamJoin = ministry.TeamJoin, AttendJoin = ministry.AttendJoin, RoundId = ministry.RoundId?.ToString() ?? "",
        ShowsInLifeGroups = ministry.ShowsInLifeGroups, Order = ministry.Order,
        PartsOff = [.. ministry.PartsOff], PartsFromBelow = [.. ministry.PartsFromBelow], PartsOrder = MinistryParts.Ordered(ministry.PartsOrder),
        ChatStarters = ChatStarters.IsValid(ministry.ChatStarters) ? ministry.ChatStarters : ChatStarters.Leaders,
    };

    public static MinistrySummary Summary(MinistryRecord ministry, string yours = "", string latest = "", int waiting = 0) => new()
    {
        Id = ministry.Id.ToString(), ParentId = ministry.ParentId?.ToString() ?? "", Name = ministry.Name,
        Summary = ministry.Summary, Meets = ministry.Meets, Colour = ministry.Colour, Icon = ministry.Icon, TeamJoin = ministry.TeamJoin,
        AttendJoin = ministry.AttendJoin, Order = ministry.Order, Yours = yours, RoundId = ministry.RoundId?.ToString() ?? "",
        ShowsInLifeGroups = ministry.ShowsInLifeGroups, Latest = latest, Waiting = waiting,
    };

    public static Post Post(PostRecord post, string from = "") => new()
    {
        Id = post.Id.ToString(), MinistryId = post.MinistryId.ToString(), Title = post.Title, Body = post.Body, Audience = post.Audience,
        Important = post.Important, Author = post.AuthorName, PostedAt = post.PostedAt, EditedAt = post.EditedAt, From = from,
        WholeChurch = post.WholeChurchAskedAt is null ? "" : "Asked to send to the whole church",
    };

    public static FileItem File(FileRecord file, string from = "") => new()
    {
        Id = file.Id.ToString(), MinistryId = file.MinistryId.ToString(), Title = file.Title, Kind = file.Kind, Url = file.Url,
        MediaId = file.MediaId?.ToString() ?? "", Audience = file.Audience, AddedAt = file.AddedAt, Note = file.Note, From = from,
    };

    public static Round Round(RoundRecord round) => new()
    {
        Id = round.Id.ToString(), Name = round.Name, Sundays = round.Sundays.Order().Select(day => day.ToString("yyyy-MM-dd")).ToList(),
        Starts = round.Starts.ToString("HH:mm"), Ends = round.Ends.ToString("HH:mm"), ShowsFrom = round.ShowsFrom?.ToString("yyyy-MM-dd") ?? "",
    };

    public static SittingOut SittingOut(SitOutRecord sitOut, RoundRecord? round, MinistryTree tree) => new()
    {
        GroupId = sitOut.GroupId.ToString(), RoundId = sitOut.RoundId.ToString(), Note = sitOut.Note,
        InsteadId = sitOut.InsteadId is { } instead && tree.Find(instead) is not null ? instead.ToString() : "",
        InsteadName = sitOut.InsteadId is { } other ? tree.Find(other)?.Name ?? "" : "",
        Round = round is null ? null : Round(round),
    };

    public static SessionChange Session(SessionRecord session) => new() { Date = session.Date.ToString("yyyy-MM-dd"), Off = session.Off, Note = session.Note };
}
