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
        new(await db.Ministries.AsNoTracking().ToListAsync(cancellationToken));

    public async Task<GrantSet> GrantsAsync(Guid? userId, CancellationToken cancellationToken) =>
        userId is { } person ? await access.GrantsForAsync(person, cancellationToken) : GrantSet.Empty;

    public static bool Holds(GrantSet grants, Permission permission, Guid ministryId) => grants.IsAllowed(permission, ministryId.ToString());

    public static bool MaySee(string audience, GrantSet grants, MinistryTree tree, Guid ministryId) =>
        MinistryRules.MaySee(
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

    public async Task<List<PersonLine>> LinesAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(ids, cancellationToken);
        return ids.Select(id => new PersonLine { Id = id.ToString(), Name = names.GetValueOrDefault(id) ?? "Someone" })
            .OrderBy(line => line.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<List<Post>> PostsAsync(MinistryTree tree, Guid ministryId, GrantSet grants, CancellationToken cancellationToken)
    {
        List<MinistryRecord> places = [tree.Find(ministryId)!, .. tree.Above(ministryId)];
        List<Guid> ids = places.Select(place => place.Id).ToList();
        List<PostRecord> records = await db.Posts.AsNoTracking().Where(post => ids.Contains(post.MinistryId))
            .OrderByDescending(post => post.PostedAt).Take(200).ToListAsync(cancellationToken);
        return records
            .Where(post => post.MinistryId == ministryId || post.Audience != Audiences.Everyone)
            .Where(post => MaySee(post.Audience, grants, tree, post.MinistryId))
            .Select(post => Post(post, post.MinistryId == ministryId ? "" : tree.Find(post.MinistryId)?.Name ?? ""))
            .ToList();
    }

    public async Task<List<FileItem>> FilesAsync(MinistryTree tree, Guid ministryId, GrantSet grants, CancellationToken cancellationToken)
    {
        List<Guid> ids = [ministryId, .. tree.Above(ministryId).Select(place => place.Id)];
        List<FileRecord> records = await db.Files.AsNoTracking().Where(file => ids.Contains(file.MinistryId))
            .OrderBy(file => file.Title).ToListAsync(cancellationToken);
        return records
            .Where(file => file.MinistryId == ministryId || file.Audience != Audiences.Everyone)
            .Where(file => MaySee(file.Audience, grants, tree, file.MinistryId))
            .Select(file => File(file, file.MinistryId == ministryId ? "" : tree.Find(file.MinistryId)?.Name ?? ""))
            .ToList();
    }

    public async Task<List<CalendarLine>> CalendarsAsync(Guid ministryId, GrantSet grants, bool signedIn, CancellationToken cancellationToken)
    {
        Viewer viewer = Viewer.Of(grants, signedIn);
        return (await calendars.OfAsync(ministryId, cancellationToken))
            .Where(calendar => calendar.Audience.Includes(viewer))
            .Select(Calendar)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, FamilyFacts>> FactsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await families.FactsAsync(ids, cancellationToken);

    public static CalendarLine Calendar(MinistryCalendar calendar) => new()
    {
        Id = calendar.Id.ToString(), Name = calendar.Name, Slug = calendar.Slug, Audience = AudienceOf(calendar.Audience),
    };

    public static string AudienceOf(Audience audience) => audience.Node switch
    {
        MinistriesPermissions.TeamViewNode => Audiences.Team,
        MinistriesPermissions.ParentsViewNode => Audiences.Parents,
        MinistriesPermissions.AttendeesViewNode => Audiences.Attendees,
        _ => Audiences.Everyone,
    };

    public static Audience CalendarAudience(string audience, Guid ministryId) =>
        Audiences.NodeOf(audience) is { } node ? Audience.HoldingAt(node, Scope.Of(ScopeKind.Ministry, ministryId.ToString())) : Audience.Everyone;

    public static Permission Permission(string node) =>
        MinistriesPermissions.All.FirstOrDefault(permission => permission.Node == node) ?? new Permission(node, ScopeKind.Ministry, node);

    public static MinistryDetails Details(MinistryRecord ministry) => new()
    {
        Id = ministry.Id.ToString(), ParentId = ministry.ParentId?.ToString() ?? "", Kind = ministry.Kind, Name = ministry.Name,
        Summary = ministry.Summary, About = ministry.About, Meets = ministry.Meets, WhoFor = ministry.WhoFor, Colour = ministry.Colour,
        Icon = ministry.Icon, TeamJoin = ministry.TeamJoin, AttendJoin = ministry.AttendJoin, RoundId = ministry.RoundId?.ToString() ?? "",
        Room = ministry.Room, JoinAsParent = ministry.JoinAsParent, Order = ministry.Order,
    };

    public static MinistrySummary Summary(MinistryRecord ministry, string yours = "", string latest = "", int waiting = 0) => new()
    {
        Id = ministry.Id.ToString(), ParentId = ministry.ParentId?.ToString() ?? "", Kind = ministry.Kind, Name = ministry.Name,
        Summary = ministry.Summary, Meets = ministry.Meets, Colour = ministry.Colour, Icon = ministry.Icon, TeamJoin = ministry.TeamJoin,
        AttendJoin = ministry.AttendJoin, Order = ministry.Order, Yours = yours, RoundId = ministry.RoundId?.ToString() ?? "",
        Room = ministry.Room, JoinAsParent = ministry.JoinAsParent, Latest = latest, Waiting = waiting,
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

    public static SessionChange Session(SessionRecord session) => new() { Date = session.Date.ToString("yyyy-MM-dd"), Off = session.Off, Note = session.Note };
}
