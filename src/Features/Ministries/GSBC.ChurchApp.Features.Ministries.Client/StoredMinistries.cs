using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class StoredMinistries(ILocalFeeds feeds, IAccess access, TimeProvider time) : IMinistryReads
{
    private const int Page = 500;
    private const int MostPosts = 200;

    public IReadOnlyList<string> Feeds => MinistriesFeeds.Published;

    public async Task<MinistryDirectory> DirectoryAsync()
    {
        MinistryMap map = await MapAsync();
        YourMinistries? yours = await YoursAsync();
        Dictionary<string, string> words = Words(map, map.All, yours);
        Dictionary<string, string> latest = [];
        foreach (string id in words.Keys.Where(id => MinistryLogic.IsOn(map.Find(id)!.Ministry, MinistryParts.News)))
            if ((await ItemsAsync<Post>(MinistriesFeeds.Posts, MinistriesFeeds.PrivatePosts, [id]))
                .Where(post => MaySee(map, post.Audience, post.MinistryId)).MaxBy(post => post.PostedAt) is { } newest)
                latest[id] = newest.Title;
        Dictionary<string, int> waiting = (await PeopleRowsAsync()).ToDictionary(row => row.MinistryId, row => row.People.Asks.Count);
        return new MinistryDirectory
        {
            Ministries = map.All.Select(card => Summary(card.Ministry, words.GetValueOrDefault(card.Ministry.Id) ?? "", latest.GetValueOrDefault(card.Ministry.Id) ?? "",
                    access.Can(MinistriesPermissions.People, card.Ministry.Id) ? waiting.GetValueOrDefault(card.Ministry.Id) : 0, access.Can(MinistriesPermissions.Settings, card.Ministry.Id)))
                .ToList(),
            MayCreate = access.Can(MinistriesPermissions.Create),
        };
    }

    public async Task<MinistryPage> MinistryAsync(string id)
    {
        MinistryMap map = await MapAsync();
        MinistryCard card = map.Find(id) ?? throw new MinistryNotHereException("That ministry isn't here any more.");
        MinistryDetails ministry = card.Ministry;
        List<string> above = map.Showing(id, MinistryParts.News);
        List<string> below = [.. map.Gathered(id, MinistryParts.News).Where(place => place != id)];
        List<string> fileIds = map.Showing(id, MinistryParts.Files);
        List<string> calendarIds = [.. map.Gathered(id, MinistryParts.Calendar)];

        MinistryPage page = new()
        {
            Ministry = ministry,
            Leaders = card.Leaders,
            SubMinistries = map.ChildrenOf(id).Select(child => Summary(child.Ministry)).ToList(),
            Path = map.Above(id).AsEnumerable().Reverse().Select(place => new Crumb { Id = place.Ministry.Id, Name = place.Ministry.Name }).ToList(),
            Posts = (await ItemsAsync<Post>(MinistriesFeeds.Posts, MinistriesFeeds.PrivatePosts, [.. above, .. below]))
                .OrderByDescending(post => post.PostedAt).Take(MostPosts)
                .Where(post => post.MinistryId == id || below.Contains(post.MinistryId) || post.Audience != Audiences.Everyone)
                .Where(post => MaySee(map, post.Audience, post.MinistryId))
                .Select(post => From(post, map, id))
                .ToList(),
            Files = (await ItemsAsync<FileItem>(MinistriesFeeds.Files, MinistriesFeeds.PrivateFiles, fileIds))
                .OrderBy(file => file.Title, StringComparer.Ordinal)
                .Where(file => file.MinistryId == id || file.Audience != Audiences.Everyone)
                .Where(file => MaySee(map, file.Audience, file.MinistryId))
                .Select(file => From(file, map, id))
                .ToList(),
            Calendars = (await ItemsAsync<CalendarLine>(MinistriesFeeds.Calendars, MinistriesFeeds.PrivateCalendars, calendarIds))
                .Where(calendar => !calendar.Linked)
                .OrderBy(calendar => calendarIds.IndexOf(calendar.MinistryId))
                .Select(calendar => From(calendar, map, id))
                .ToList(),
            You = await YouAsync(map, ministry),
        };
        List<DaySpan>? within = null;
        if (ministry.ShowsInLifeGroups)
        {
            page.Round = (await AllAsync<Round>(MinistriesFeeds.Rounds)).FirstOrDefault(round => round.Id == ministry.RoundId);
            string today = ChurchTime.Today(time).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            IReadOnlyList<SittingOut> sittingOut = await AllAsync<SittingOut>(MinistriesFeeds.SittingOut);
            page.SittingOut = sittingOut
                .Where(sitOut => sitOut.GroupId == id && sitOut.Round is { Sundays.Count: > 0 } round && string.CompareOrdinal(round.Sundays.Max(), today) >= 0)
                .OrderBy(sitOut => sitOut.Round!.Sundays.Min(), StringComparer.Ordinal)
                .ToList();
            IReadOnlyList<Round> rounds = await AllAsync<Round>(MinistriesFeeds.Rounds);
            page.Sundays = LifeGroupRounds.SundaysOf(id, ministry.RoundId, rounds, sittingOut);
            within = LifeGroupRounds.SpansOf(id, ministry.RoundId, rounds, sittingOut);
        }
        if (MinistryParts.IsOn(ministry.PartsOff, MinistryParts.Calendar))
            page.Calendars.AddRange((await ItemsAsync<CalendarLine>(MinistriesFeeds.Calendars, MinistriesFeeds.PrivateCalendars, [id]))
                .Where(calendar => calendar.Linked && calendar.MinistryId == id)
                .OrderBy(calendar => calendar.Name, StringComparer.Ordinal)
                .Select(calendar => OnlyWithin(calendar, within)));
        return page;
    }

    private static CalendarLine OnlyWithin(CalendarLine calendar, IReadOnlyList<DaySpan>? within)
    {
        (calendar.Within, calendar.OnlyWithin) = ([.. within ?? []], within is not null);
        return calendar;
    }

    public async Task<LifeGroupsPage> LifeGroupsAsync()
    {
        MinistryMap map = await MapAsync();
        List<MinistryCard> groups = map.All.Where(card => card.Ministry.ShowsInLifeGroups).ToList();
        List<string> groupIds = groups.Select(group => group.Ministry.Id).ToList();
        Dictionary<string, string> words = Words(map, groups, await YoursAsync());
        return new LifeGroupsPage
        {
            CalendarName = (await AllAsync<LifeGroupsCalendar>(MinistriesFeeds.LifeGroups)).FirstOrDefault()?.CalendarName ?? "",
            SittingOut = (await AllAsync<SittingOut>(MinistriesFeeds.SittingOut)).Where(sitOut => groupIds.Contains(sitOut.GroupId)).Select(WithoutRound).ToList(),
            Rounds = (await AllAsync<Round>(MinistriesFeeds.Rounds)).OrderBy(round => round.Sundays.DefaultIfEmpty("").Min(StringComparer.Ordinal), StringComparer.Ordinal).ToList(),
            Groups = groups.Select(group => Summary(group.Ministry, words.GetValueOrDefault(group.Ministry.Id) ?? "")).ToList(),
            MayPlan = access.Can(MinistriesPermissions.PlanLifeGroups),
            Under = groups.Select(group => group.Ministry.ParentId).Where(parent => parent.Length > 0).Distinct()
                .Select(map.Find).OfType<MinistryCard>().Select(parent => new Crumb { Id = parent.Ministry.Id, Name = parent.Ministry.Name }).ToList(),
        };
    }

    public async Task<PeopleList> PeopleAsync(string ministryId)
    {
        MinistryPeople row = (await PeopleRowsAsync()).FirstOrDefault(people => people.MinistryId == ministryId)
                             ?? throw new MinistryNotHereException("Its people show here once your phone has them.");
        PeopleList people = row.People;
        people.MayGive = access.Can(MinistriesPermissions.Settings, ministryId) ? [Roles.Leader, Roles.Admin, Roles.Team, Roles.Attendee] : [];
        return people;
    }

    private async Task<YourPlace> YouAsync(MinistryMap map, MinistryDetails ministry)
    {
        YourPlace you = new() { SignedIn = access.SignedIn };
        if (!access.SignedIn)
            return you;
        YourMinistries yours = await YoursAsync() ?? new YourMinistries();
        YourRoles place = yours.Places.FirstOrDefault(each => each.MinistryId == ministry.Id) ?? new YourRoles();
        you.Roles = [.. place.Roles];
        bool leadsHere = you.Roles.Contains(Roles.Leader) || you.Roles.Contains(Roles.Admin);
        you.LeadsFromAbove = !leadsHere && access.Can(MinistriesPermissions.Settings, ministry.Id);
        you.ParentOfAttendee = access.Can(MinistriesPermissions.ParentsView, ministry.Id) && !you.Roles.Contains(Roles.Team) && !leadsHere;
        List<AskLine> here = yours.Asked.Where(ask => ask.MinistryId == ministry.Id).ToList();
        you.Children = yours.Children.Select(child => new ChildPlace
            {
                Id = child.Id, Name = child.Name, Age = child.Age, Attends = place.ChildrenAttending.Contains(child.Id), OnTeam = place.ChildrenOnTeam.Contains(child.Id),
                Asked = here.Any(ask => ask.PersonId == child.Id),
            })
            .ToList();
        you.Asked = here.Where(ask => ask.PersonId == yours.PersonId || ask.AskedById == yours.PersonId).ToList();
        you.Minor = yours.Minor;
        you.ForYourYes = here.Where(ask => ask.WaitingForParent && ask.AskedById != yours.PersonId && yours.Children.Any(child => child.Id == ask.PersonId)).ToList();
        if (access.Can(MinistriesPermissions.Post, ministry.Id))
            you.MayPostFor = [.. MinistryLogic.PostableAudiences(ministry.AttendJoin)];
        you.MayManagePeople = access.Can(MinistriesPermissions.People, ministry.Id);
        you.MayEditSettings = access.Can(MinistriesPermissions.Settings, ministry.Id);
        you.MayDelete = ministry.ParentId.Length > 0 ? access.Can(MinistriesPermissions.Settings, ministry.ParentId) : access.Can(MinistriesPermissions.Create);
        if (you.MayManagePeople)
            you.Waiting = (await PeopleRowsAsync()).FirstOrDefault(row => row.MinistryId == ministry.Id)?.People.Asks.Count ?? 0;
        return you;
    }

    private Dictionary<string, string> Words(MinistryMap map, IEnumerable<MinistryCard> among, YourMinistries? yours)
    {
        Dictionary<string, string> words = [];
        if (yours is null)
            return words;
        foreach (MinistryCard card in among)
        {
            string id = card.Ministry.Id;
            if (!access.Can(MinistriesPermissions.AttendeesView, id) && !access.Can(MinistriesPermissions.TeamView, id))
                continue;
            YourRoles place = yours.Places.FirstOrDefault(each => each.MinistryId == id) ?? new YourRoles();
            List<string> attending = access.Can(MinistriesPermissions.ParentsView, id)
                ? yours.Children.Where(child => place.ChildrenAttending.Contains(child.Id)).Select(child => child.Name).ToList()
                : [];
            if (place.Roles.Count == 0 && access.Can(MinistriesPermissions.Settings, id))
                continue;
            string said = MinistryLogic.Yours(place.Roles, attending, leadsFromAbove: false);
            if (said.Length > 0)
                words[id] = said;
        }
        return words;
    }

    private static SittingOut WithoutRound(SittingOut sitOut)
    {
        sitOut.Round = null;
        return sitOut;
    }

    private bool MaySee(MinistryMap map, string audience, string ministryId) =>
        MinistryLogic.MaySee(
            audience,
            node => Holds(node, ministryId),
            node => map.Below(ministryId).Any(below => Holds(node, below.Ministry.Id)));

    private bool Holds(string node, string ministryId) =>
        access.Can(MinistriesPermissions.All.FirstOrDefault(permission => permission.Node == node) ?? new Permission(node, ScopeKind.Ministry, node), ministryId);

    private static Post From(Post post, MinistryMap map, string id) => With(post, post.MinistryId, id, map, (item, from) => item.From = from);

    private static FileItem From(FileItem file, MinistryMap map, string id) => With(file, file.MinistryId, id, map, (item, from) => item.From = from);

    private static CalendarLine From(CalendarLine calendar, MinistryMap map, string id) => With(calendar, calendar.MinistryId, id, map, (item, from) => item.From = from);

    private static T With<T>(T item, string ministryId, string id, MinistryMap map, Action<T, string> from)
    {
        from(item, ministryId == id ? "" : map.Find(ministryId)?.Ministry.Name ?? "");
        return item;
    }

    private static MinistrySummary Summary(MinistryDetails ministry, string yours = "", string latest = "", int waiting = 0, bool mayAddUnder = false) => new()
    {
        Id = ministry.Id, ParentId = ministry.ParentId, Name = ministry.Name, Summary = ministry.Summary, Meets = ministry.Meets, Colour = ministry.Colour,
        Icon = ministry.Icon, TeamJoin = ministry.TeamJoin, AttendJoin = ministry.AttendJoin, Order = ministry.Order, Yours = yours, RoundId = ministry.RoundId,
        ShowsInLifeGroups = ministry.ShowsInLifeGroups, Latest = latest, Waiting = waiting, MayAddUnder = mayAddUnder,
    };

    private async Task<MinistryMap> MapAsync() => new(await AllAsync<MinistryCard>(MinistriesFeeds.Directory));

    private async Task<YourMinistries?> YoursAsync() =>
        access.SignedIn ? (await AllAsync<YourMinistries>(MinistriesFeeds.Yours)).FirstOrDefault() : null;

    private async Task<List<MinistryPeople>> PeopleRowsAsync() =>
        access.SignedIn ? await AllAsync<MinistryPeople>(MinistriesFeeds.People) : [];

    private async Task<List<T>> ItemsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string publicFeed, string privateFeed, IEnumerable<string> ministryIds)
        where T : class
    {
        List<T> items = [];
        foreach (string ministryId in ministryIds.Distinct())
        {
            items.AddRange(await AllAsync<T>(publicFeed, ministryId));
            if (access.SignedIn)
                items.AddRange(await AllAsync<T>(privateFeed, ministryId));
        }
        return items.DistinctBy(IdOf).ToList();
    }

    private static string IdOf<T>(T item) => item switch
    {
        Post post => post.Id,
        FileItem file => file.Id,
        CalendarLine calendar => calendar.Id,
        _ => "",
    };

    private async Task<List<T>> AllAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string feed, string? group = null)
    {
        List<T> all = [];
        for (int offset = 0; ; offset += Page)
        {
            IReadOnlyList<StoredRecord> page = await feeds.ReadPageAsync(new FeedPageQuery(feed, Limit: Page, Offset: offset, GroupKey: group));
            all.AddRange(page.Select(record => MinistriesContractsModel.Instance.Deserialize<T>(new MemoryStream(record.Payload))));
            if (page.Count < Page)
                return all;
        }
    }
}
