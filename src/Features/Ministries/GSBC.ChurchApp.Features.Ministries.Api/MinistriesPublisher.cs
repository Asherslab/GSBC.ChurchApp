using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistriesPublisher(
    MinistriesDbContext db, MinistryViews views, IFamilies families, IPeopleIndex people, IMinistryCalendars calendars, IKeptRegularEvents kept, TimeProvider time)
{
    private const long AdvisoryLockKey = 0x6d69_6e69_7374_7279;
    public const int PostsKept = 100;
    public const string LifeGroupsRow = "calendar";

    internal sealed record Row(string Feed, string RecordId, byte[] Payload, string? Audience = null, string? GroupKey = null);

    public async Task PublishAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({AdvisoryLockKey})", cancellationToken);

        Dictionary<(string, string), Row> wanted = (await RowsAsync(cancellationToken)).ToDictionary(row => (row.Feed, row.RecordId));
        List<FeedChangeRecord> latest = await db.FeedChanges.AsNoTracking()
            .Where(change => MinistriesFeeds.Published.Contains(change.Feed))
            .GroupBy(change => new { change.Feed, change.RecordId })
            .Select(versions => versions.OrderByDescending(change => change.Cursor).First())
            .ToListAsync(cancellationToken);
        Dictionary<(string, string), FeedChangeRecord> published = latest.Where(change => !change.Deleted).ToDictionary(change => (change.Feed, change.RecordId));

        DateTime now = time.GetUtcNow().UtcDateTime;
        long version = now.Ticks;
        HashSet<string> peopleChanged = [];
        foreach ((string feed, string recordId) in published.Keys.Where(key => !wanted.ContainsKey(key)))
        {
            db.FeedChanges.Add(new FeedChangeRecord { Feed = feed, RecordId = recordId, Version = version, Deleted = true });
            if (SignalsPeople(feed))
                peopleChanged.Add(recordId);
        }
        foreach (((string, string) key, Row row) in wanted)
        {
            if (published.TryGetValue(key, out FeedChangeRecord? before) && before.ScopeId == row.Audience && before.GroupKey == row.GroupKey
                && before.Payload.AsSpan().SequenceEqual(row.Payload))
                continue;
            db.FeedChanges.Add(new FeedChangeRecord
            {
                Feed = row.Feed, RecordId = row.RecordId, Version = version, Payload = row.Payload, ScopeId = row.Audience, GroupKey = row.GroupKey,
            });
            if (SignalsPeople(row.Feed))
                peopleChanged.Add(row.RecordId);
        }
        if (peopleChanged.Count > 0)
        {
            MinistryTree tree = await views.TreeAsync(cancellationToken);
            foreach (Guid ministry in peopleChanged.Select(Guid.Parse).SelectMany(changed => tree.Below(changed).Select(below => below.Id).Prepend(changed)).Distinct())
                db.FeedChanges.Add(MinistryPeopleChanges.Signal(ministry, now));
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static bool SignalsPeople(string feed) => feed is MinistriesFeeds.People or MinistriesFeeds.Directory;

    public async Task<IReadOnlyList<Row>> RowsAsync(CancellationToken cancellationToken)
    {
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        MinistryHoldings holdings = await views.HoldingsAsync(tree.All, cancellationToken);
        HashSet<Guid> active = [.. tree.All.Select(ministry => ministry.Id)];
        List<PostRecord> posts = (await db.Posts.AsNoTracking().ToListAsync(cancellationToken)).Where(post => active.Contains(post.MinistryId)).ToList();
        List<FileRecord> files = (await db.Files.AsNoTracking().ToListAsync(cancellationToken)).Where(file => active.Contains(file.MinistryId)).ToList();
        List<AskRecord> asks = (await db.Asks.AsNoTracking().OrderBy(ask => ask.AskedAt).ToListAsync(cancellationToken)).Where(ask => active.Contains(ask.MinistryId)).ToList();
        List<SessionRecord> sessions = (await db.Sessions.AsNoTracking().ToListAsync(cancellationToken)).Where(session => active.Contains(session.GroupId)).ToList();
        List<SitOutRecord> sitOuts = (await db.SitOuts.AsNoTracking().ToListAsync(cancellationToken)).Where(sitOut => active.Contains(sitOut.GroupId)).ToList();
        List<RoundRecord> rounds = await db.Rounds.AsNoTracking().ToListAsync(cancellationToken);
        Dictionary<Guid, RoundRecord> roundsById = rounds.ToDictionary(round => round.Id);
        IReadOnlyList<MinistryCalendar> shownCalendars = await calendars.OfAsync(tree.All.Select(ministry => ministry.Id).ToList(), cancellationToken);

        List<Guid> everyone = [.. (await people.EveryoneAsync(cancellationToken)), .. holdings.People, .. asks.SelectMany(ask => new[] { ask.PersonId, ask.AskedBy })];
        IReadOnlyDictionary<Guid, FamilyFacts> facts = await families.FactsAsync(everyone.Distinct().ToList(), cancellationToken);
        List<Guid> named = [.. everyone, .. facts.Values.SelectMany(fact => fact.Parents.Concat(fact.Children))];
        IReadOnlyDictionary<Guid, string> names = await people.NamesAsync(named.Distinct().ToList(), cancellationToken);
        List<AskLine> askLines = await MinistriesService.AskLinesAsync(asks, tree, views, people, cancellationToken);

        List<Row> rows = [];
        foreach (MinistryRecord ministry in tree.All)
        {
            rows.Add(Public(MinistriesFeeds.Directory, ministry.Id, new MinistryCard
            {
                Ministry = MinistryViews.Details(ministry),
                Leaders = Leaders(tree, ministry, holdings, names),
            }));
            PeopleList list = MinistryPeopleLists.Build(ministry, tree, holdings, facts, names,
                askLines.Where(ask => ask.MinistryId == ministry.Id.ToString()).ToList(), []);
            rows.Add(Encoded(MinistriesFeeds.People, ministry.Id.ToString(), new MinistryPeople { MinistryId = ministry.Id.ToString(), People = list },
                At(MinistriesPermissions.PeopleNode, ministry.Id).Text));
        }

        foreach (PostRecord post in posts.GroupBy(post => post.MinistryId).SelectMany(each => each.OrderByDescending(post => post.PostedAt).Take(PostsKept)))
            rows.AddRange(ForAudience(MinistriesFeeds.Posts, MinistriesFeeds.PrivatePosts, post.Id, post.MinistryId, post.Audience, tree, MinistryViews.Post(post)));
        foreach (FileRecord file in files)
            rows.AddRange(ForAudience(MinistriesFeeds.Files, MinistriesFeeds.PrivateFiles, file.Id, file.MinistryId, file.Audience, tree, MinistryViews.File(file)));
        foreach (MinistryCalendar calendar in shownCalendars.Where(calendar => !calendar.Hidden))
            rows.Add(Encoded(calendar.Audience == Audience.Everyone ? MinistriesFeeds.Calendars : MinistriesFeeds.PrivateCalendars, calendar.Id.ToString(),
                    MinistryViews.Calendar(calendar), calendar.Audience == Audience.Everyone ? null : calendar.Audience.Text) with { GroupKey = calendar.MinistryId.ToString() });

        rows.AddRange(rounds.Select(round => Public(MinistriesFeeds.Rounds, round.Id, MinistryViews.Round(round))));
        rows.AddRange(sessions.GroupBy(session => session.GroupId).Select(group => Public(MinistriesFeeds.Sessions, group.Key, new GroupSessions
        {
            GroupId = group.Key.ToString(), Changes = group.OrderBy(session => session.Date).Select(MinistryViews.Session).ToList(),
        })));

        rows.AddRange(sitOuts.Select(sitOut => Encoded(MinistriesFeeds.SittingOut, $"{sitOut.GroupId}@{sitOut.RoundId}",
            MinistryViews.SittingOut(sitOut, roundsById.GetValueOrDefault(sitOut.RoundId), tree))));
        rows.Add(Encoded(MinistriesFeeds.LifeGroups, LifeGroupsRow,
            new LifeGroupsCalendar { CalendarName = (await kept.WhereAsync(LifeGroupSundays.Keeper, cancellationToken))?.Name ?? "" }));

        foreach (Guid person in everyone.Distinct())
            if (Yours(person, tree, holdings, facts, names, askLines) is { } yours)
                rows.Add(Encoded(MinistriesFeeds.Yours, person.ToString(), yours, person.ToString()));
        return rows;
    }

    public static YourMinistries? Yours(
        Guid person, MinistryTree tree, MinistryHoldings holdings, IReadOnlyDictionary<Guid, FamilyFacts> facts, IReadOnlyDictionary<Guid, string> names,
        IReadOnlyList<AskLine> asks)
    {
        List<Guid> children = (facts.GetValueOrDefault(person)?.Children ?? []).Where(child => facts.GetValueOrDefault(child)?.Minor != false).ToList();
        string id = person.ToString();
        List<string> childIds = children.Select(child => child.ToString()).ToList();
        YourMinistries yours = new()
        {
            PersonId = id,
            Minor = facts.GetValueOrDefault(person)?.Minor == true,
            Children = children.Select(child => new ChildFacts { Id = child.ToString(), Name = names.GetValueOrDefault(child) ?? "Your child", Age = facts.GetValueOrDefault(child)?.Age })
                .OrderBy(child => child.Name, StringComparer.CurrentCulture).ToList(),
            Places = tree.All.Select(ministry => new YourRoles
                {
                    MinistryId = ministry.Id.ToString(),
                    Roles = [.. holdings.RolesOf(person, ministry.Id)],
                    ChildrenAttending = children.Where(holdings.Of(Roles.Attendee, ministry.Id).Contains).Select(child => child.ToString()).ToList(),
                    ChildrenOnTeam = children.Where(holdings.Of(Roles.Team, ministry.Id).Contains).Select(child => child.ToString()).ToList(),
                })
                .Where(place => place.Roles.Count + place.ChildrenAttending.Count + place.ChildrenOnTeam.Count > 0)
                .OrderBy(place => place.MinistryId, StringComparer.Ordinal)
                .ToList(),
            Asked = asks.Where(ask => ask.PersonId == id || childIds.Contains(ask.PersonId) || AskedBy(ask, person)).ToList(),
        };
        return yours.Minor || yours.Children.Count + yours.Places.Count + yours.Asked.Count > 0 ? yours : null;
    }

    private static bool AskedBy(AskLine ask, Guid person) => ask.AskedById == person.ToString();

    private static List<PersonLine> Leaders(MinistryTree tree, MinistryRecord ministry, MinistryHoldings holdings, IReadOnlyDictionary<Guid, string> names)
    {
        foreach (Guid place in new[] { ministry.Id }.Concat(tree.Above(ministry.Id).Select(above => above.Id)))
        {
            IReadOnlyList<Guid> leaders = holdings.Of(Roles.Leader, place);
            if (leaders.Count > 0)
                return leaders.Select(leader => new PersonLine { Id = leader.ToString(), Name = names.GetValueOrDefault(leader) ?? "Someone" })
                    .OrderBy(line => line.Name, StringComparer.CurrentCulture).ToList();
        }
        return [];
    }

    private static IEnumerable<Row> ForAudience<T>(string publicFeed, string privateFeed, Guid id, Guid ministryId, string audience, MinistryTree tree, T item)
    {
        string group = ministryId.ToString();
        if (audience == Audiences.Everyone)
        {
            yield return Public(publicFeed, id, item) with { GroupKey = group };
            yield break;
        }
        if (Audiences.NodeOf(audience) is not { } node)
            yield break;
        if (audience != Audiences.Team)
        {
            yield return Encoded(privateFeed, id.ToString(), item, At(node, ministryId).Text) with { GroupKey = group };
            yield break;
        }
        foreach (Guid place in new[] { ministryId }.Concat(tree.Below(ministryId).Select(below => below.Id)))
            yield return Encoded(privateFeed, place == ministryId ? id.ToString() : $"{id}@{place}", item, At(node, place).Text) with { GroupKey = group };
    }

    private static Audience At(string node, Guid ministryId) => Audience.HoldingAt(node, Scope.Of(ScopeKind.Ministry, ministryId.ToString()));

    private static Row Public<T>(string feed, Guid id, T item) => Encoded(feed, id.ToString(), item);

    private static Row Encoded<T>(string feed, string recordId, T item, string? audience = null)
    {
        using MemoryStream stream = new();
        MinistriesContractsModel.Instance.Serialize(stream, item);
        return new Row(feed, recordId, stream.ToArray(), audience);
    }
}
