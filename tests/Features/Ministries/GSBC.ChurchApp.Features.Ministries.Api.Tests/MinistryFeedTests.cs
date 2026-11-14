using System.Globalization;
using System.Security.Claims;
using System.Text;
using Grpc.Core;
using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Client;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Api.News;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Api.Content;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.News;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

[Integration("Publishing reads every ministry, post and role from Postgres and writes the phones' feed rows there, which Sync filters per person")]
public sealed class MinistryFeedTests : IAsyncLifetime
{
    private readonly TestClock _clock = new();
    private readonly Holdings _roles = new();
    private readonly People _people = new();
    private readonly Told _told = new();
    private readonly NoCalendars _calendars = new();
    private readonly Dictionary<Guid, GrantSet> _grants = [];
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    private readonly MinistryRecord _youth;
    private readonly MinistryRecord _band;
    private readonly Guid _leader;
    private readonly Guid _bandMember;
    private readonly Guid _visitor;

    public MinistryFeedTests()
    {
        _youth = Ministry("Youth", null);
        _band = Ministry("Youth Band", _youth.Id);
        _leader = _people.Add("Lee");
        _bandMember = _people.Add("Tess");
        _visitor = _people.Add("Olly");
    }

    private readonly KeptPostMedia _media = new();
    private readonly RecordedChurchNews _churchNews = new();

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
                .Build())
            .AddSingleton<TimeProvider>(_clock)
            .AddLogging()
            .AddSingleton<IRoleAssignments>(_roles)
            .AddSingleton<IPeopleIndex>(_people)
            .AddSingleton<IFamilies>(_people)
            .AddSingleton<IMinistryCalendars>(_calendars)
            .AddSingleton<IAccessEvaluator>(new Granted(_grants))
            .AddSingleton<INotifications>(_told)
            .AddSingleton<IKeptRegularEvents>(new KeptInSundays())
            .AddScoped<MinistryViews>()
            .AddScoped<MinistryChanges>()
            .AddScoped<MinistriesPublisher>()
            .AddScoped<MinistriesService>()
            .AddSingleton<IChurchNews>(_churchNews)
            .AddScoped(_ => new MinistryPostMedia(_media, _media, _media))
            .AddScoped<MinistryChurchNews>()
            .AddScoped(provider => new AppCardChecks([new MinistryCardSource(provider.GetRequiredService<MinistryViews>())], new PlaceAccess(provider.GetRequiredService<IAccessEvaluator>())))
            .AddScoped(provider => new MinistryLeadingService(provider.GetRequiredService<MinistriesDbContext>(), provider.GetRequiredService<AppCardChecks>(), provider.GetRequiredService<MinistryViews>(),
                provider.GetRequiredService<MinistryChanges>(), null!, null!, _roles, _people, _people, _calendars, new KeptInSundays(), _told, null!,
                provider.GetRequiredService<MinistryPostMedia>(), provider.GetRequiredService<MinistryChurchNews>(), provider.GetRequiredService<IAccessEvaluator>(), _clock))
            .AddMinistriesData()
            .AddSyncData()
            .BuildServiceProvider();
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
        await db.Database.MigrateAsync(Cancel);

        _roles.Give(Roles.Leader, _leader, _youth.Id);
        _roles.Give(Roles.Team, _bandMember, _band.Id);
        _grants[_leader] = Grants((_youth.Id, MinistriesApiExtensions.LeaderPatterns), (_band.Id, MinistriesApiExtensions.LeaderPatterns));
        _grants[_bandMember] = Grants((_band.Id, [MinistriesPermissions.TeamViewNode, MinistriesPermissions.AttendeesViewNode, MinistriesPermissions.ParentsViewNode]));
        _grants[_visitor] = GrantSet.Empty;

        RoundRecord term = new()
        {
            Id = Guid.CreateVersion7(), Name = "Term 4", Sundays = [ChurchDay(7), ChurchDay(14)], Starts = new TimeOnly(9, 30), Ends = new TimeOnly(10, 15),
        };
        _band.ShowsInLifeGroups = true;
        _band.RoundId = term.Id;
        db.Rounds.Add(term);
        db.SitOuts.Add(new SitOutRecord { GroupId = _band.Id, RoundId = term.Id, Note = "Camp weekend", MarkedAt = _clock.UtcNow });
        db.Ministries.AddRange(_youth, _band);
        db.Posts.AddRange(Post(_youth, "Welcome, everyone", Audiences.Everyone, 1), Post(_youth, "Team rehearsal moved", Audiences.Team, 2));
        await db.SaveChangesAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<MinistriesPublisher>().PublishAsync(Cancel);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task ATeamOnlyPostNeverReachesThePhoneOfSomeoneNotOnTheTeam()
    {
        Phone visitor = await PhoneOfAsync(_visitor);
        Phone bandMember = await PhoneOfAsync(_bandMember);

        Assert.Equal(["Welcome, everyone"], visitor.Posts().Select(post => post.Title).Distinct());
        Assert.Equal(["Team rehearsal moved", "Welcome, everyone"], bandMember.Posts().Select(post => post.Title).Distinct().Order());
        Assert.Equal(["Welcome, everyone"], (await visitor.Ministries.MinistryAsync(_youth.Id.ToString())).Posts.Select(post => post.Title));
    }

    [Fact]
    public async Task AMinistryOpensFromThePhonesStoreAsTheServerWouldShowIt()
    {
        foreach (Guid person in new[] { _visitor, _bandMember, _leader })
        {
            Phone phone = await PhoneOfAsync(person);
            foreach (MinistryRecord ministry in new[] { _youth, _band })
                Assert.Equal(Bytes(await ServerPageAsync(person, ministry.Id)), Bytes(await phone.Ministries.MinistryAsync(ministry.Id.ToString())));
            Assert.Equal(Bytes(await ServerDirectoryAsync(person)), Bytes(await phone.Ministries.DirectoryAsync()));
            Assert.Equal(Bytes(await ServerLifeGroupsAsync(person)), Bytes(await phone.Ministries.LifeGroupsAsync()));
        }
    }

    [Fact]
    public async Task ALifeGroupsLinkedCalendarReachesEveryPhoneFilteredToItsSetsDatesAsTheServerShowsIt()
    {
        MinistryCalendar services = new(Guid.CreateVersion7(), Guid.Empty, "Services", "services", Audience.Everyone, Hidden: false);
        _calendars.ForEveryone.Add(services);
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MinistryLeadingService>()
                .LinkCalendarAsync(new CalendarLinkSave { MinistryId = _band.Id.ToString(), CalendarId = services.Id.ToString(), Linked = true }, Call(_leader));
            await scope.ServiceProvider.GetRequiredService<MinistriesPublisher>().PublishAsync(Cancel);
        }

        foreach (Guid person in new[] { _visitor, _bandMember, _leader })
        {
            MinistryPage server = await ServerPageAsync(person, _band.Id);
            CalendarLine linked = Assert.Single(server.Calendars, calendar => calendar.Linked);
            Assert.True(linked.OnlyWithin);
            DaySpan within = Assert.Single(linked.Within);
            Assert.Equal((ChurchDay(7).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), ChurchDay(14).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), (within.From, within.To));
            Assert.Equal(Bytes(server), Bytes(await (await PhoneOfAsync(person)).Ministries.MinistryAsync(_band.Id.ToString())));
        }
    }

    [Fact]
    public async Task APostAboutASundayMustBeOneOfTheGroupsOwnSundaysAndReachesThePhoneWithIt()
    {
        string sunday = ChurchDay(14).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            MinistryLeadingService leading = scope.ServiceProvider.GetRequiredService<MinistryLeadingService>();
            Post NoClassOn(string day) => new() { Title = SundayPosts.NoClass, Audience = Audiences.Everyone, Sunday = day, NoClass = true };

            RpcException notItsSunday = await Assert.ThrowsAsync<RpcException>(async () =>
                await leading.SavePostAsync(new PostSave { MinistryId = _band.Id.ToString(), Post = NoClassOn(ChurchDay(21).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) }, Call(_leader)));
            RpcException notALifeGroup = await Assert.ThrowsAsync<RpcException>(async () =>
                await leading.SavePostAsync(new PostSave { MinistryId = _youth.Id.ToString(), Post = NoClassOn(sunday) }, Call(_leader)));
            Assert.Equal([StatusCode.InvalidArgument, StatusCode.InvalidArgument], [notItsSunday.StatusCode, notALifeGroup.StatusCode]);

            await leading.SavePostAsync(new PostSave { MinistryId = _band.Id.ToString(), Post = NoClassOn(sunday) }, Call(_leader));
            await scope.ServiceProvider.GetRequiredService<MinistriesPublisher>().PublishAsync(Cancel);
        }

        Post onThePhone = Assert.Single((await (await PhoneOfAsync(_visitor)).Ministries.MinistryAsync(_band.Id.ToString())).Posts);
        Assert.Equal((sunday, true), (onThePhone.Sunday, onThePhone.NoClass));
    }

    [Fact]
    public async Task APostKeepsItsFormattingOnThePhoneAndOlderAppsGetItsPlainText()
    {
        Post saved = await SavePostAsync(_leader, _youth, NewPost("Lunch after church", Audiences.Everyone, "**Bring** a plate, see [the roster](/events)."));

        Assert.Equal("Bring a plate, see the roster.", saved.Body);
        Post onThePhone = Assert.Single((await (await PhoneOfAsync(_visitor)).Ministries.MinistryAsync(_youth.Id.ToString())).Posts, post => post.Id == saved.Id);
        Assert.Equal("**Bring** a plate, see [the roster](/events).", onThePhone.Content?.Markdown);
    }

    [Fact]
    public async Task ALeadersAskGoesToChurchNewsOnlyForAPostForEveryoneAndLeavesWhenItStopsBeingOne()
    {
        Post teamOnly = await SavePostAsync(_leader, _youth, NewPost("Leaders' meeting", Audiences.Team, ask: true));
        Assert.Empty(_churchNews.Calls);
        Assert.Equal(ChurchNewsStates.None, teamOnly.ChurchNews);

        Post asked = await SavePostAsync(_leader, _youth, NewPost("Holiday program", Audiences.Everyone, ask: true));
        Assert.Equal(("ask", Guid.Parse(asked.Id)), (_churchNews.Calls[^1].What, _churchNews.Calls[^1].SourceId));
        Assert.Equal(("Youth", "/ministries/" + _youth.Id + "/news"), (_churchNews.Calls[^1].Post!.FromName, _churchNews.Calls[^1].Post!.Link));
        Assert.Equal(ChurchNewsStates.Asked, asked.ChurchNews);

        asked.New = false;
        asked.Audience = Audiences.Team;
        Post narrowed = await SavePostAsync(_leader, _youth, asked);

        Assert.Equal(("withdraw", Guid.Parse(asked.Id)), (_churchNews.Calls[^1].What, _churchNews.Calls[^1].SourceId));
        Assert.Equal(ChurchNewsStates.None, narrowed.ChurchNews);
    }

    [Fact]
    public async Task OnlyChurchNewsPublishersShareAMinistrysPostAndDeletingItTakesItOutOfChurchNews()
    {
        Guid staff = Guid.CreateVersion7();
        _grants[staff] = new GrantSet([new Grant(PermissionPattern.Parse(ChurchNews.PublishNode), Scope.Global)]);
        Post post = await SavePostAsync(_leader, _youth, NewPost("Holiday program", Audiences.Everyone));

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            MinistryLeadingService leading = scope.ServiceProvider.GetRequiredService<MinistryLeadingService>();
            ChurchNewsShare share = new() { MinistryId = _youth.Id.ToString(), Id = post.Id, Tell = true };
            RpcException refused = await Assert.ThrowsAsync<RpcException>(async () => await leading.ShareToChurchNewsAsync(share, Call(_leader)));
            Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);

            Post shared = await leading.ShareToChurchNewsAsync(share, Call(staff));
            Assert.Equal(ChurchNewsStates.Shared, shared.ChurchNews);

            await leading.DeletePostAsync(new ItemRef { MinistryId = _youth.Id.ToString(), Id = post.Id }, Call(_leader));
        }

        Assert.Equal(["share", "withdraw"], _churchNews.Calls.Select(call => call.What));
    }

    private static Post NewPost(string title, string audience, string text = "", bool ask = false) => new()
    {
        Id = Guid.NewGuid().ToString(), New = true, Title = title, Audience = audience, Content = new RichText { Markdown = text }, AskWholeChurch = ask,
    };

    private async Task<Post> SavePostAsync(Guid person, MinistryRecord ministry, Post post)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        Post saved = await scope.ServiceProvider.GetRequiredService<MinistryLeadingService>()
            .SavePostAsync(new PostSave { MinistryId = ministry.Id.ToString(), Post = post }, Call(person));
        await scope.ServiceProvider.GetRequiredService<MinistriesPublisher>().PublishAsync(Cancel);
        return saved;
    }

    [Fact]
    public async Task AYoungPersonJoiningATeamWaitsForAParentsYesAndItsLeadersHearOfIt()
    {
        Guid teen = _people.Child("Tom Young", _visitor);
        _grants[teen] = GrantSet.Empty;
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<MinistriesDbContext>().Ministries.Where(ministry => ministry.Id == _youth.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(ministry => ministry.TeamJoin, Joining.Open), Cancel);
            JoinOutcome outcome = await scope.ServiceProvider.GetRequiredService<MinistriesService>()
                .JoinAsync(new JoinRequest { MinistryId = _youth.Id.ToString(), As = Places.Team }, Call(teen));
            Assert.False(outcome.Joined);
            await scope.ServiceProvider.GetRequiredService<MinistriesPublisher>().PublishAsync(Cancel);
        }
        Assert.False(_roles.Holds(Roles.Team, teen, _youth.Id));
        Assert.Contains(_told.Raised, told => told.Kind == MinistriesNotifications.ForYourYes && told.Audience == NotificationAudience.Person(_visitor));
        Assert.Contains(_told.Raised, told => told.Kind == MinistriesNotifications.Asked && told.Body.Contains("a parent says yes first", StringComparison.Ordinal));

        MinistryPage parents = await ServerPageAsync(_visitor, _youth.Id);
        AskLine waiting = Assert.Single(parents.You.ForYourYes);
        Assert.Equal(Bytes(parents), Bytes(await (await PhoneOfAsync(_visitor)).Ministries.MinistryAsync(_youth.Id.ToString())));
        await Assert.ThrowsAsync<RpcException>(async () => await ParentAnswerAsync(teen, waiting.Id));

        await ParentAnswerAsync(_visitor, waiting.Id);

        Assert.True(_roles.Holds(Roles.Team, teen, _youth.Id));
        Assert.Contains(_told.Raised, told => told.Kind == MinistriesNotifications.YoungJoined && told.Body.Contains("Olly said yes", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OneRoleChoiceMovesSomeoneBetweenRolesOrChangesNothingWhenItCant()
    {
        _roles.Givable.UnionWith([Roles.KeyOf(Roles.Team), Roles.KeyOf(Roles.Attendee)]);
        await SetRoleAsync(_bandMember, Roles.Attendee);
        Assert.Equal((false, true), (_roles.Holds(Roles.Team, _bandMember, _band.Id), _roles.Holds(Roles.Attendee, _bandMember, _band.Id)));

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => SetRoleAsync(_bandMember, Roles.Leader));

        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);
        Assert.Equal((false, true), (_roles.Holds(Roles.Leader, _bandMember, _band.Id), _roles.Holds(Roles.Attendee, _bandMember, _band.Id)));
    }

    private async Task SetRoleAsync(Guid person, string role)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MinistryLeadingService>()
            .SetRoleAsync(new PersonChange { MinistryId = _band.Id.ToString(), PersonId = person.ToString(), Role = role }, Call(_leader));
    }

    private async Task ParentAnswerAsync(Guid person, string askId)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MinistriesService>().ParentAnswerAsync(new AskRef { AskId = askId, Yes = true }, Call(person));
    }

    [Fact]
    public async Task SomeoneJoiningATeamSignalsThatMinistryAndThoseUnderItAtOnceAndNothingElse()
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        SyncDbContext sync = scope.ServiceProvider.GetRequiredService<SyncDbContext>();
        MinistriesPublisher publisher = scope.ServiceProvider.GetRequiredService<MinistriesPublisher>();
        long before = await MinistryPeopleChanges.LatestAsync(sync, Cancel);

        await publisher.PublishAsync(Cancel);
        Assert.Empty((await MinistryPeopleChanges.SinceAsync(sync, before, Cancel)).Ministries);

        _roles.Give(Roles.Team, _visitor, _youth.Id);
        await publisher.PublishAsync(Cancel);

        Assert.Equal(new[] { _youth.Id, _band.Id }.Order(), (await MinistryPeopleChanges.SinceAsync(sync, before, Cancel)).Ministries.Order());
    }

    [Fact]
    public async Task APostsCardsPointOnlyAtMinistriesAndLifeGroupsItsWriterCanSee()
    {
        MinistryRecord office = Ministry("Church office", null);
        office.Hidden = true;
        await AddAsync([office]);

        await SavePostAsync(_leader, _youth, NewPost("Join the band", Audiences.Everyone, $"Come along.\n\n::lifegroup{{id={_band.Id}}}\n\n::ministry{{id={_youth.Id} look=box}}"));

        RpcException notAGroup = await Assert.ThrowsAsync<RpcException>(() => SavePostAsync(_leader, _youth, NewPost("Youth", Audiences.Everyone, $"::lifegroup{{id={_youth.Id}}}")));
        RpcException hidden = await Assert.ThrowsAsync<RpcException>(() => SavePostAsync(_leader, _youth, NewPost("Office", Audiences.Everyone, $"::ministry{{id={office.Id}}}")));
        RpcException gone = await Assert.ThrowsAsync<RpcException>(() => SavePostAsync(_leader, _youth, NewPost("Gone", Audiences.Everyone, $"::ministry{{id={Guid.CreateVersion7()}}}")));
        Assert.All([notAGroup, hidden, gone], refused => Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode));
        Assert.Equal(gone.Status.Detail, hidden.Status.Detail);
    }

    [Fact]
    public async Task AHiddenMinistryAndEverythingInItReachOnlyThePhonesOfItsOwnPeople()
    {
        MinistryRecord office = Ministry("Church office", null);
        office.Hidden = true;
        MinistryRecord accounts = Ministry("Accounts", office.Id);
        Guid bookkeeper = _people.Add("Bea");
        _roles.Give(Roles.Team, bookkeeper, accounts.Id);
        _grants[bookkeeper] = Grants((accounts.Id, TeamNodes));
        await AddAsync([office, accounts], Post(office, "Office hours over Christmas", Audiences.Everyone, 3), Post(accounts, "Receipts due Friday", Audiences.Team, 4));

        foreach (Guid outsider in new[] { _visitor, _bandMember, _leader })
        {
            Phone phone = await PhoneOfAsync(outsider);
            Assert.DoesNotContain(phone.Stored.Values.SelectMany(rows => rows.Values), row => Mentions(row, office) || Mentions(row, accounts));
            Assert.DoesNotContain((await phone.Ministries.DirectoryAsync()).Ministries, ministry => ministry.Id == office.Id.ToString() || ministry.Id == accounts.Id.ToString());
            await Assert.ThrowsAsync<MinistryNotHereException>(() => phone.Ministries.MinistryAsync(office.Id.ToString()));
            RpcException refused = await Assert.ThrowsAsync<RpcException>(() => ServerPageAsync(outsider, accounts.Id));
            Assert.Equal(StatusCode.NotFound, refused.StatusCode);
            Assert.Equal(Bytes(await ServerDirectoryAsync(outsider)), Bytes(await phone.Ministries.DirectoryAsync()));
        }

        Phone bea = await PhoneOfAsync(bookkeeper);
        Assert.All((await bea.Ministries.DirectoryAsync()).Ministries.Where(ministry => ministry.Id == office.Id.ToString() || ministry.Id == accounts.Id.ToString()),
            ministry => Assert.True(ministry.Hidden));
        Assert.Equal(["Office hours over Christmas", "Receipts due Friday"], bea.Posts().Select(post => post.Title).Where(title => title != "Welcome, everyone").Distinct().Order());
        Assert.Equal(Bytes(await ServerDirectoryAsync(bookkeeper)), Bytes(await bea.Ministries.DirectoryAsync()));
        foreach (MinistryRecord ministry in new[] { office, accounts })
            Assert.Equal(Bytes(await ServerPageAsync(bookkeeper, ministry.Id)), Bytes(await bea.Ministries.MinistryAsync(ministry.Id.ToString())));
    }

    [Fact]
    public async Task NobodyOutsideAHiddenMinistryCanAskToJoinIt()
    {
        MinistryRecord office = Ministry("Church office", null);
        office.Hidden = true;
        await AddAsync([office]);

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        RpcException refused = await Assert.ThrowsAsync<RpcException>(async () => await scope.ServiceProvider.GetRequiredService<MinistriesService>()
            .JoinAsync(new JoinRequest { MinistryId = office.Id.ToString(), As = Places.Team }, Call(_visitor)));
        Assert.Equal(StatusCode.NotFound, refused.StatusCode);
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<MinistriesDbContext>().Asks.ToListAsync(Cancel));
    }

    [Fact]
    public async Task HidingAMinistryTakesItsCalendarForEveryoneToItsOwnPeopleAndShowingItGivesItBack()
    {
        MinistryRecord prayer = Ministry("Prayer team", null);
        await AddAsync([prayer]);
        Scope at = Scope.Of(ScopeKind.Ministry, prayer.Id.ToString());
        MinistryCalendar dates = new(Guid.CreateVersion7(), prayer.Id, "Prayer nights", "prayer-nights", Audience.Everyone, Hidden: false);
        MinistryCalendar team = new(Guid.CreateVersion7(), prayer.Id, "Prayer team", "prayer-team", Audience.HoldingAt(MinistriesPermissions.TeamViewNode, at), Hidden: false);
        _calendars.Owned.AddRange([dates, team]);

        await SaveHiddenAsync(prayer, hidden: true);
        Assert.Equal(Audience.HoldingAt(MinistriesPermissions.SeeHiddenNode, at), _calendars.Owned.Single(calendar => calendar.Id == dates.Id).Audience);
        Assert.Equal(team.Audience, _calendars.Owned.Single(calendar => calendar.Id == team.Id).Audience);
        Assert.DoesNotContain((await PhoneOfAsync(_visitor)).Stored.Values.SelectMany(rows => rows.Values), row => Mentions(row, prayer));

        await SaveHiddenAsync(prayer, hidden: false);
        Assert.Equal(Audience.Everyone, _calendars.Owned.Single(calendar => calendar.Id == dates.Id).Audience);
        Assert.Contains((await PhoneOfAsync(_visitor)).Stored[MinistriesFeeds.Directory].Values, row => Mentions(row, prayer));
    }

    private static readonly IReadOnlyList<string> TeamNodes =
        [MinistriesPermissions.TeamViewNode, MinistriesPermissions.AttendeesViewNode, MinistriesPermissions.ParentsViewNode, MinistriesPermissions.SeeHiddenNode];

    private static bool Mentions(FeedChangeRecord row, MinistryRecord ministry) =>
        row.RecordId.Contains(ministry.Id.ToString(), StringComparison.Ordinal) || row.GroupKey == ministry.Id.ToString()
        || Encoding.UTF8.GetString(row.Payload).Contains(ministry.Name, StringComparison.Ordinal);

    private async Task AddAsync(IEnumerable<MinistryRecord> ministries, params PostRecord[] posts)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
        db.Ministries.AddRange(ministries);
        db.Posts.AddRange(posts);
        await db.SaveChangesAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<MinistriesPublisher>().PublishAsync(Cancel);
    }

    private async Task SaveHiddenAsync(MinistryRecord ministry, bool hidden)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        MinistryDetails details = MinistryViews.Details(ministry);
        details.Hidden = hidden;
        await scope.ServiceProvider.GetRequiredService<MinistryLeadingService>().SaveMinistryAsync(new MinistrySave { MinistryId = details.Id, Details = details }, Call(_leader));
        await scope.ServiceProvider.GetRequiredService<MinistriesPublisher>().PublishAsync(Cancel);
    }

    private async Task<Phone> PhoneOfAsync(Guid person)
    {
        ClaimsPrincipal user = User(person);
        Granted access = new(_grants);
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
        List<FeedChangeRecord> all = await db.FeedChanges.AsNoTracking().Where(change => MinistriesFeeds.Published.Contains(change.Feed)).OrderBy(change => change.Cursor).ToListAsync(Cancel);
        Dictionary<string, Dictionary<string, FeedChangeRecord>> stored = [];
        foreach (FeedDefinition feed in FeedsOnTheServer())
        {
            Dictionary<string, FeedChangeRecord> rows = stored[feed.Name] = [];
            if (!await FeedVisibility.CanSeeFeedAsync(feed, user, access, Cancel))
                continue;
            foreach (FeedChangeRecord row in await FeedVisibility.VisibleRowsAsync(feed, all.Where(change => change.Feed == feed.Name).ToList(), user, access, Cancel))
                if (row.Deleted)
                    rows.Remove(row.RecordId);
                else
                    rows[row.RecordId] = row;
        }
        return new Phone(stored, new StoredMinistries(new Store(stored), new PhoneAccess(_grants[person]), _clock));
    }

    private static IEnumerable<FeedDefinition> FeedsOnTheServer()
    {
        ServiceCollection services = new();
        services.AddMinistriesApi();
        return services.Where(service => service.ServiceType == typeof(FeedDefinition)).Select(service => (FeedDefinition)service.ImplementationInstance!)
            .Where(feed => MinistriesFeeds.Published.Contains(feed.Name));
    }

    private async Task<MinistryPage> ServerPageAsync(Guid person, Guid ministry)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MinistriesService>().MinistryAsync(new MinistryRef { MinistryId = ministry.ToString() }, Call(person));
    }

    private async Task<LifeGroupsPage> ServerLifeGroupsAsync(Guid person)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MinistriesService>().LifeGroupsAsync(new LifeGroupsRequest(), Call(person));
    }

    private async Task<MinistryDirectory> ServerDirectoryAsync(Guid person)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<MinistriesService>().DirectoryAsync(new DirectoryRequest(), Call(person));
    }

    private static string Bytes<T>(T message)
    {
        using MemoryStream stream = new();
        MinistriesContractsModel.Instance.Serialize(stream, message);
        return Convert.ToBase64String(stream.ToArray());
    }

    private static CallContext Call(Guid person)
    {
        ClaimsPrincipal user = User(person);
        return new CallContext(user, new Context(user));
    }

    private static ClaimsPrincipal User(Guid person) =>
        new(new ClaimsIdentity([new Claim(Caller.UserIdClaim, person.ToString()), new Claim(Caller.SessionIdClaim, person.ToString())], "test"));

    private static GrantSet Grants(params (Guid Ministry, IReadOnlyList<string> Nodes)[] places) =>
        new(places.SelectMany(place => place.Nodes.Select(node => new Grant(PermissionPattern.Parse(node), Scope.Of(ScopeKind.Ministry, place.Ministry.ToString())))).ToList());

    private MinistryRecord Ministry(string name, Guid? parent) => new()
    {
        Id = Guid.CreateVersion7(), ParentId = parent, Name = name, TeamJoin = Joining.Ask, AttendJoin = Joining.None,
        CreatedAt = _clock.UtcNow, UpdatedAt = _clock.UtcNow,
    };

    private DateOnly ChurchDay(int daysAhead) => ChurchTime.Today(_clock).AddDays(daysAhead);

    private PostRecord Post(MinistryRecord ministry, string title, string audience, int minutes) => new()
    {
        Id = Guid.CreateVersion7(), MinistryId = ministry.Id, Title = title, Audience = audience, AuthorId = _leader, AuthorName = "Lee",
        PostedAt = _clock.UtcNow.AddMinutes(minutes),
    };

    private sealed record Phone(Dictionary<string, Dictionary<string, FeedChangeRecord>> Stored, StoredMinistries Ministries)
    {
        public IEnumerable<Post> Posts() =>
            new[] { MinistriesFeeds.Posts, MinistriesFeeds.PrivatePosts }.SelectMany(feed => Stored[feed].Values)
                .Select(row => MinistriesContractsModel.Instance.Deserialize<Post>(new MemoryStream(row.Payload)));
    }

    private sealed class Store(Dictionary<string, Dictionary<string, FeedChangeRecord>> stored) : ILocalFeeds
    {
        public bool IsAvailable => true;

        public SyncState State => SyncState.Offline;

        public event Action<IReadOnlySet<string>>? FeedsChanged { add { } remove { } }

        public event Action? StateChanged { add { } remove { } }

        public void Follow(IEnumerable<string> feeds) { }

        public ValueTask<IReadOnlyList<StoredRecord>> ReadPageAsync(FeedPageQuery query) =>
            ValueTask.FromResult<IReadOnlyList<StoredRecord>>(stored.GetValueOrDefault(query.Feed, []).Values
                .Where(row => query.GroupKey is null || row.GroupKey == query.GroupKey)
                .OrderBy(row => row.Cursor)
                .Skip(query.Offset).Take(query.Limit)
                .Select(row => new StoredRecord(row.RecordId, row.Version, row.Payload, row.SortKey, row.StartsAt, row.EndsAt, row.GroupKey))
                .ToList());

        public ValueTask<StoreInfo> InfoAsync() => throw new NotSupportedException();

        public ValueTask RefreshAsync() => throw new NotSupportedException("The phone is offline.");
    }

    private sealed class PhoneAccess(GrantSet grants) : IAccess
    {
        public event Action? Changed { add { } remove { } }

        public bool SignedIn => true;

        public bool Can(Permission permission, string? objectId = null) => grants.IsAllowed(permission, objectId);

        public bool CanSomewhere(Permission permission) => grants.IsAllowedSomewhere(permission);
    }

    private sealed class Granted(Dictionary<Guid, GrantSet> grants) : IAccessEvaluator
    {
        public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Caller.UserId(user) is { } id && grants.GetValueOrDefault(id, GrantSet.Empty).IsAllowed(new Permission(node, ScopeKind.Ministry, node), scopeId));

        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(grants.GetValueOrDefault(userId, GrantSet.Empty));
    }

    private sealed class Holdings : IRoleAssignments
    {
        private readonly List<(string Role, Guid Person, string Place)> _held = [];

        public void Give(string role, Guid person, Guid place) => _held.Add((Roles.KeyOf(role), person, place.ToString()));

        public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<RoleHolder>>(_held.Where(held => held.Role == roleKey && held.Place == objectId).Select(held => new RoleHolder(held.Person)).ToList());

        public HashSet<string> Givable { get; } = [];

        public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) => ValueTask.FromResult(Givable.Contains(roleKey));

        public ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask ForgetScopeAsync(ScopeKind kind, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<int> HoldingCountAsync(string roleKey, string objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
        {
            _held.Add((roleKey, userId, objectId ?? ""));
            return ValueTask.FromResult(AssignmentOutcome.Success);
        }

        public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken)
        {
            _held.Remove((roleKey, userId, objectId ?? ""));
            return ValueTask.FromResult(AssignmentOutcome.Success);
        }

        public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
        {
            _held.Add((roleKey, userId, objectId ?? ""));
            return ValueTask.CompletedTask;
        }

        public bool Holds(string role, Guid person, Guid place) => _held.Contains((Roles.KeyOf(role), person, place.ToString()));

        public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class People : IPeopleIndex, IFamilies
    {
        private readonly Dictionary<Guid, string> _names = [];
        private readonly Dictionary<Guid, Guid> _parentOf = [];

        public Guid Child(string name, Guid parent)
        {
            Guid id = Add(name);
            _parentOf[id] = parent;
            return id;
        }

        public Guid Add(string name)
        {
            Guid id = Guid.CreateVersion7();
            _names[id] = name;
            return id;
        }

        public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<Guid>>([.. _names.Keys]);

        public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyDictionary<Guid, string>>(people.Where(_names.ContainsKey).Distinct().ToDictionary(person => person, person => _names[person]));

        public ValueTask<IReadOnlyDictionary<Guid, FamilyFacts>> FactsAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyDictionary<Guid, FamilyFacts>>(people.Distinct().ToDictionary(person => person, person => _parentOf.TryGetValue(person, out Guid parent)
                ? new FamilyFacts(person, 14, true, [parent], [])
                : new FamilyFacts(person, null, true, [], _parentOf.Where(each => each.Value == person).Select(each => each.Key).ToList())));

        public ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<IReadOnlySet<Guid>> NotInvitedYetAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<PersonReach> ReachAsync(Guid personId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<bool> MayActForAsync(Guid actorId, Guid personId, CancellationToken cancellationToken) => ValueTask.FromResult(actorId == personId);

        public ValueTask LinkAsync(Guid parentId, Guid childId, Guid linkedBy, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class NoCalendars : IMinistryCalendars
    {
        public List<MinistryCalendar> ForEveryone { get; } = [];

        public List<MinistryCalendar> Owned { get; } = [];

        public ValueTask<IReadOnlyList<MinistryCalendar>> ForEveryoneAsync(CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<MinistryCalendar>>(ForEveryone);

        public ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(Guid ministryId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<MinistryCalendar>>(Owned.Where(calendar => calendar.MinistryId == ministryId).ToList());

        public ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<MinistryCalendar>>(Owned.Where(calendar => ministryIds.Contains(calendar.MinistryId)).ToList());

        public ValueTask ShowAsync(Guid ministryId, bool shown, Audience whileHidden, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<MinistryCalendar> CreateAsync(Guid ministryId, string name, Audience audience, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<MinistryCalendar> ChangeAsync(Guid ministryId, Guid calendarId, string name, Audience audience, CancellationToken cancellationToken)
        {
            int at = Owned.FindIndex(calendar => calendar.Id == calendarId && calendar.MinistryId == ministryId);
            Owned[at] = Owned[at] with { Name = name, Audience = audience };
            return ValueTask.FromResult(Owned[at]);
        }

        public ValueTask<MinistryCalendarRemoval> WhatGoesAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask RemoveAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class KeptInSundays : IKeptRegularEvents
    {
        public ValueTask<KeptCalendar?> WhereAsync(string keeper, CancellationToken cancellationToken) =>
            ValueTask.FromResult<KeptCalendar?>(new KeptCalendar(Guid.CreateVersion7(), "Sundays"));

        public ValueTask<IReadOnlyList<KeptCalendar>> ChoicesAsync(ClaimsPrincipal user, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask PutInAsync(ClaimsPrincipal user, Guid calendarId, KeptRegularEvent wanted, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask KeepInStepAsync(KeptRegularEvent wanted, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask RemoveAsync(string keeper, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Told : INotifications
    {
        public List<Notification> Raised { get; } = [];

        public Task RaiseAsync(Notification notification, CancellationToken cancellationToken)
        {
            Raised.Add(notification);
            return Task.CompletedTask;
        }

        public Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task SettleAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task CollectAsync(NotificationDigestItem item, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task ForgetDigestAsync(NotificationKind kind, string group, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class Context(ClaimsPrincipal person) : ServerCallContext
    {
        private readonly Dictionary<object, object> _state = new() { ["__HttpContext"] = new DefaultHttpContext { User = person } };

        protected override IDictionary<object, object> UserStateCore => _state;
        protected override string MethodCore => "";
        protected override string HostCore => "";
        protected override string PeerCore => "";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => TestContext.Current.CancellationToken;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(null, []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
