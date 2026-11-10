using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistriesService(
    MinistriesDbContext db,
    MinistryViews views,
    MinistryChanges changes,
    IRoleAssignments roles,
    IFamilies families,
    IPeopleIndex people,
    INotifications notifications,
    TimeProvider time) : IMinistriesService
{
    private const int LongestNote = 500;

    public async ValueTask<MinistryDirectory> DirectoryAsync(DirectoryRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid? me = Me(context);
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        GrantSet grants = await views.GrantsAsync(me, cancellationToken);
        Dictionary<Guid, string> yours = me is { } person ? await YoursAsync(person, tree, grants, cancellationToken) : [];
        Dictionary<Guid, string> latest = await LatestAsync(tree, yours.Keys, grants, cancellationToken);
        Dictionary<Guid, int> waiting = me is null ? [] : (await db.Asks.AsNoTracking().GroupBy(ask => ask.MinistryId)
                .Select(group => new { group.Key, Count = group.Count() }).ToListAsync(cancellationToken))
            .Where(group => MinistryViews.Holds(grants, MinistriesPermissions.People, group.Key))
            .ToDictionary(group => group.Key, group => group.Count);
        Guid? lifeGroups = await views.LifeGroupsHomeAsync(tree, cancellationToken);
        return new MinistryDirectory
        {
            Ministries = tree.All.OrderBy(ministry => ministry.Order).ThenBy(ministry => ministry.Name, StringComparer.CurrentCulture)
                .Select(ministry => MayAddUnder(MinistryViews.Summary(ministry, yours.GetValueOrDefault(ministry.Id) ?? "", latest.GetValueOrDefault(ministry.Id) ?? "",
                    waiting.GetValueOrDefault(ministry.Id)), ministry, grants))
                .ToList(),
            MayCreate = grants.IsAllowed(MinistriesPermissions.Create),
            LifeGroupsId = lifeGroups?.ToString() ?? "",
        };
    }

    private static MinistrySummary MayAddUnder(MinistrySummary summary, MinistryRecord ministry, GrantSet grants)
    {
        summary.MayAddUnder = ministry.Kind != MinistryKinds.LifeGroup && MinistryViews.Holds(grants, MinistriesPermissions.Settings, ministry.Id);
        return summary;
    }

    public async ValueTask<MinistryPage> MinistryAsync(MinistryRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid? me = Me(context);
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        MinistryRecord ministry = Existing(tree, request.MinistryId);
        GrantSet grants = await views.GrantsAsync(me, cancellationToken);
        MinistryPage page = new()
        {
            Ministry = MinistryViews.Details(ministry),
            Leaders = await views.LeadersAsync(tree, ministry.Id, cancellationToken),
            SubMinistries = tree.ChildrenOf(ministry.Id).Select(child => MinistryViews.Summary(child)).ToList(),
            Path = tree.Above(ministry.Id).Reverse().Select(above => new Crumb { Id = above.Id.ToString(), Name = above.Name }).ToList(),
            Posts = await views.PostsAsync(tree, ministry.Id, grants, cancellationToken),
            Files = await views.FilesAsync(tree, ministry.Id, grants, cancellationToken),
            Calendars = await views.CalendarsAsync(tree, ministry.Id, grants, me is not null, cancellationToken),
            You = await YouAsync(me, tree, ministry, grants, cancellationToken),
        };
        if (ministry.Kind == MinistryKinds.LifeGroup)
        {
            page.Sessions = (await db.Sessions.AsNoTracking().Where(session => session.GroupId == ministry.Id).OrderBy(session => session.Date).ToListAsync(cancellationToken))
                .Select(MinistryViews.Session).ToList();
            if (ministry.RoundId is { } roundId && await db.Rounds.AsNoTracking().SingleOrDefaultAsync(found => found.Id == roundId, cancellationToken) is { } round)
                page.Round = MinistryViews.Round(round);
        }
        return page;
    }

    public async ValueTask<LifeGroupsPage> LifeGroupsAsync(LifeGroupsRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        List<MinistryRecord> groups = tree.All.Where(ministry => ministry.Kind == MinistryKinds.LifeGroup).OrderBy(group => group.Order).ThenBy(group => group.Name).ToList();
        Guid? home = await views.LifeGroupsHomeAsync(tree, cancellationToken);
        List<Guid> groupIds = groups.Select(group => group.Id).ToList();
        List<SessionRecord> sessions = await db.Sessions.AsNoTracking().Where(session => groupIds.Contains(session.GroupId)).ToListAsync(cancellationToken);
        GrantSet grants = await views.GrantsAsync(Me(context), cancellationToken);
        return new LifeGroupsPage
        {
            MinistryId = home?.ToString() ?? "",
            Rounds = home is { } parent
                ? (await db.Rounds.AsNoTracking().Where(round => round.MinistryId == parent).ToListAsync(cancellationToken))
                    .OrderBy(round => round.Sundays.DefaultIfEmpty().Min()).Select(MinistryViews.Round).ToList()
                : [],
            Groups = groups.Select(group => MinistryViews.Summary(group)).ToList(),
            Sessions = sessions.GroupBy(session => session.GroupId)
                .Select(each => new GroupSessions { GroupId = each.Key.ToString(), Changes = each.OrderBy(session => session.Date).Select(MinistryViews.Session).ToList() })
                .ToList(),
            MaySetUp = home is { } lifeGroups && MinistryViews.Holds(grants, MinistriesPermissions.Settings, lifeGroups),
        };
    }

    public async ValueTask<JoinOutcome> JoinAsync(JoinRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = SignedIn(context);
        Guid person = await ForWhomAsync(me, request.PersonId, cancellationToken);
        if (!Places.IsValid(request.As))
            throw Invalid("Choose whether it's to join the team or to come along.");
        string note = request.Note.Trim();
        if (note.Length > LongestNote)
            throw Invalid($"Keep the note to {LongestNote} characters.");

        MinistryTree tree = await views.TreeAsync(cancellationToken);
        MinistryRecord ministry = Existing(tree, request.MinistryId);
        string role = request.As == Places.Team ? Roles.Team : Roles.Attendee;
        if ((await views.RolesAsync(person, ministry.Id, cancellationToken)).Contains(role))
            return new JoinOutcome { Joined = true, Words = "Already in." };

        JoinDecision decision = MinistryRules.Decide(request.As == Places.Team ? ministry.TeamJoin : ministry.AttendJoin, request.As);
        if (decision.Refusal is { } refusal)
            throw Refused(refusal);
        if (decision.Joins)
        {
            await roles.GiveAsFeatureAsync(person, Roles.KeyOf(role), ministry.Id.ToString(), null, cancellationToken);
            changes.Touch(ministry.Id);
            await db.SaveChangesAsync(cancellationToken);
            return new JoinOutcome { Joined = true, Words = $"You're in {ministry.Name}." };
        }

        if (!await db.Asks.AnyAsync(ask => ask.MinistryId == ministry.Id && ask.PersonId == person && ask.As == request.As, cancellationToken))
            db.Asks.Add(new AskRecord
            {
                Id = Guid.CreateVersion7(), MinistryId = ministry.Id, PersonId = person, As = request.As, AskedBy = me, Note = note,
                AskedAt = time.GetUtcNow().UtcDateTime,
            });
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        string name = (await people.NamesAsync([person], cancellationToken)).GetValueOrDefault(person) ?? "Someone";
        await notifications.RaiseAsync(new Notification(
            MinistriesNotifications.Asked, NotificationAudience.Holding(MinistriesPermissions.People, ministry.Id.ToString()),
            $"{name} asks to {(request.As == Places.Team ? "join the team" : "come along")}", $"In {ministry.Name}. Answer in the app.",
            $"/ministries/{ministry.Id}/people", Subject: $"ministries/asks/{ministry.Id:N}"), cancellationToken);
        return new JoinOutcome { Asked = true, Words = "Asked. A leader says yes or no, and you'll hear in the app." };
    }

    public async ValueTask<Done> LeaveAsync(JoinRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = SignedIn(context);
        Guid person = await ForWhomAsync(me, request.PersonId, cancellationToken);
        MinistryRecord ministry = Existing(await views.TreeAsync(cancellationToken), request.MinistryId);
        string role = request.As == Places.Team ? Roles.Team : Roles.Attendee;
        await roles.TakeAwayAsFeatureAsync(person, Roles.KeyOf(role), ministry.Id.ToString(), cancellationToken);
        changes.Touch(ministry.Id);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<Done> WithdrawAsync(AskRef request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        Guid me = SignedIn(context);
        Guid id = Guid.TryParse(request.AskId, out Guid parsed) ? parsed : Guid.Empty;
        AskRecord ask = await db.Asks.SingleOrDefaultAsync(found => found.Id == id, cancellationToken)
                        ?? throw new RpcException(new Status(StatusCode.NotFound, "That ask was already answered."));
        if (ask.AskedBy != me && ask.PersonId != me && !await families.MayActForAsync(me, ask.PersonId, cancellationToken))
            throw new RpcException(new Status(StatusCode.PermissionDenied, "That isn't your ask to take back."));
        db.Asks.Remove(ask);
        changes.Touch(ask.MinistryId);
        await db.SaveChangesAsync(cancellationToken);
        return new Done();
    }

    public async ValueTask<WaitingList> WaitingAsync(WaitingRequest request, CallContext context = default)
    {
        CancellationToken cancellationToken = context.CancellationToken;
        GrantSet grants = await views.GrantsAsync(SignedIn(context), cancellationToken);
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        List<AskRecord> asks = (await db.Asks.AsNoTracking().OrderBy(ask => ask.AskedAt).ToListAsync(cancellationToken))
            .Where(ask => MinistryViews.Holds(grants, MinistriesPermissions.People, ask.MinistryId))
            .ToList();
        return new WaitingList { Asks = await AskLinesAsync(asks, tree, views, people, cancellationToken) };
    }

    public static async Task<List<AskLine>> AskLinesAsync(
        IReadOnlyCollection<AskRecord> asks, MinistryTree tree, MinistryViews views, IPeopleIndex people, CancellationToken cancellationToken)
    {
        List<Guid> ids = asks.SelectMany(ask => new[] { ask.PersonId, ask.AskedBy }).Distinct().ToList();
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(ids, cancellationToken);
        IReadOnlyDictionary<Guid, FamilyFacts> facts = await views.FactsAsync(asks.Select(ask => ask.PersonId).Distinct().ToList(), cancellationToken);
        return asks.Select(ask => new AskLine
        {
            Id = ask.Id.ToString(), PersonId = ask.PersonId.ToString(), Name = names.GetValueOrDefault(ask.PersonId) ?? "Someone",
            Age = facts.GetValueOrDefault(ask.PersonId)?.Age, As = ask.As,
            AskedBy = ask.AskedBy == ask.PersonId ? "" : names.GetValueOrDefault(ask.AskedBy) ?? "",
            Note = ask.Note, AskedAt = ask.AskedAt, MinistryId = ask.MinistryId.ToString(), MinistryName = tree.Find(ask.MinistryId)?.Name ?? "",
        }).ToList();
    }

    private async Task<YourPlace> YouAsync(Guid? me, MinistryTree tree, MinistryRecord ministry, GrantSet grants, CancellationToken cancellationToken)
    {
        YourPlace you = new() { SignedIn = me is not null };
        if (me is not { } person)
            return you;

        you.Roles = [.. await views.RolesAsync(person, ministry.Id, cancellationToken)];
        bool leadsHere = you.Roles.Contains(Roles.Leader) || you.Roles.Contains(Roles.Admin);
        you.LeadsFromAbove = !leadsHere && MinistryViews.Holds(grants, MinistriesPermissions.Settings, ministry.Id);
        you.ParentOfAttendee = MinistryViews.Holds(grants, MinistriesPermissions.ParentsView, ministry.Id) && !you.Roles.Contains(Roles.Team) && !leadsHere;

        FamilyFacts mine = (await views.FactsAsync([person], cancellationToken))[person];
        if (mine.Children.Count > 0)
        {
            IReadOnlyDictionary<Guid, FamilyFacts> children = await views.FactsAsync(mine.Children, cancellationToken);
            IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(mine.Children, cancellationToken);
            IReadOnlyList<Guid> attendees = await views.HoldersAsync(Roles.Attendee, ministry.Id, cancellationToken);
            IReadOnlyList<Guid> team = await views.HoldersAsync(Roles.Team, ministry.Id, cancellationToken);
            List<Guid> asked = await db.Asks.AsNoTracking().Where(ask => ask.MinistryId == ministry.Id && mine.Children.Contains(ask.PersonId))
                .Select(ask => ask.PersonId).ToListAsync(cancellationToken);
            you.Children = mine.Children
                .Where(child => children.GetValueOrDefault(child)?.Minor != false)
                .Select(child => new ChildPlace
                {
                    Id = child.ToString(), Name = names.GetValueOrDefault(child) ?? "Your child", Age = children.GetValueOrDefault(child)?.Age,
                    Attends = attendees.Contains(child), OnTeam = team.Contains(child), Asked = asked.Contains(child),
                })
                .OrderBy(child => child.Name, StringComparer.CurrentCulture)
                .ToList();
        }

        List<AskRecord> asks = await db.Asks.AsNoTracking().Where(ask => ask.MinistryId == ministry.Id && (ask.PersonId == person || ask.AskedBy == person))
            .ToListAsync(cancellationToken);
        you.Asked = await AskLinesAsync(asks, tree, views, people, cancellationToken);
        if (MinistryViews.Holds(grants, MinistriesPermissions.Post, ministry.Id))
            you.MayPostFor = [.. MinistryRules.PostableAudiences(ministry.AttendJoin)];
        you.MayManagePeople = MinistryViews.Holds(grants, MinistriesPermissions.People, ministry.Id);
        you.MayEditSettings = MinistryViews.Holds(grants, MinistriesPermissions.Settings, ministry.Id);
        you.MayDelete = ministry.ParentId is { } parent
            ? MinistryViews.Holds(grants, MinistriesPermissions.Settings, parent)
            : grants.IsAllowed(MinistriesPermissions.Create);
        if (you.MayManagePeople)
            you.Waiting = await db.Asks.CountAsync(ask => ask.MinistryId == ministry.Id, cancellationToken);
        return you;
    }

    private async Task<Dictionary<Guid, string>> YoursAsync(Guid me, MinistryTree tree, GrantSet grants, CancellationToken cancellationToken)
    {
        Dictionary<Guid, string> yours = [];
        IReadOnlyList<Guid> children = (await views.FactsAsync([me], cancellationToken))[me].Children;
        IReadOnlyDictionary<Guid, string> names = children.Count > 0 ? await people.NamesAsync(children, cancellationToken) : new Dictionary<Guid, string>();
        foreach (MinistryRecord ministry in tree.All)
        {
            bool sees = MinistryViews.Holds(grants, MinistriesPermissions.AttendeesView, ministry.Id)
                        || MinistryViews.Holds(grants, MinistriesPermissions.TeamView, ministry.Id);
            if (!sees)
                continue;
            IReadOnlyList<string> held = await views.RolesAsync(me, ministry.Id, cancellationToken);
            List<string> attending = [];
            if (children.Count > 0 && MinistryViews.Holds(grants, MinistriesPermissions.ParentsView, ministry.Id))
            {
                IReadOnlyList<Guid> attendees = await views.HoldersAsync(Roles.Attendee, ministry.Id, cancellationToken);
                attending = children.Where(attendees.Contains).Select(child => names.GetValueOrDefault(child) ?? "Your child").ToList();
            }
            bool fromAbove = held.Count == 0 && MinistryViews.Holds(grants, MinistriesPermissions.Settings, ministry.Id);
            if (fromAbove)
                continue;
            string words = MinistryRules.Yours(held, attending, leadsFromAbove: false);
            if (words.Length > 0)
                yours[ministry.Id] = words;
        }
        return yours;
    }

    private async Task<Dictionary<Guid, string>> LatestAsync(MinistryTree tree, IEnumerable<Guid> ministries, GrantSet grants, CancellationToken cancellationToken)
    {
        List<Guid> ids = ministries.Where(id => tree.Find(id) is { } ministry && MinistryViews.IsOn(ministry, MinistryParts.News)).ToList();
        if (ids.Count == 0)
            return [];
        List<PostRecord> posts = await db.Posts.AsNoTracking().Where(post => ids.Contains(post.MinistryId))
            .OrderByDescending(post => post.PostedAt).Take(100).ToListAsync(cancellationToken);
        return posts
            .Where(post => MinistryViews.MaySee(post.Audience, grants, tree, post.MinistryId))
            .GroupBy(post => post.MinistryId)
            .ToDictionary(group => group.Key, group => group.First().Title);
    }

    private async Task<Guid> ForWhomAsync(Guid me, string personId, CancellationToken cancellationToken)
    {
        if (personId.Length == 0 || personId == me.ToString())
            return me;
        Guid person = Guid.TryParse(personId, out Guid parsed) ? parsed : throw Invalid("That isn't a person in the app.");
        return await families.MayActForAsync(me, person, cancellationToken)
            ? person
            : throw new RpcException(new Status(StatusCode.PermissionDenied, "You can only do this for yourself or a child you look after."));
    }

    public static MinistryRecord Existing(MinistryTree tree, string ministryId) =>
        Guid.TryParse(ministryId, out Guid id) && tree.Find(id) is { } ministry
            ? ministry
            : throw new RpcException(new Status(StatusCode.NotFound, "That ministry isn't here any more."));

    public static Guid? Me(CallContext context)
    {
        ClaimsPrincipal user = context.ServerCallContext!.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true ? Caller.UserId(user) : null;
    }

    public static Guid SignedIn(CallContext context) =>
        Me(context) ?? throw new RpcException(new Status(StatusCode.Unauthenticated, "Sign in again."));

    public static RpcException Invalid(string message) => new(new Status(StatusCode.InvalidArgument, message));

    public static RpcException Refused(string message) => new(new Status(StatusCode.FailedPrecondition, message));
}
