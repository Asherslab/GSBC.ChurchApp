using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Client;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
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
            .AddSingleton<IMinistryCalendars>(new NoCalendars())
            .AddSingleton<IAccessEvaluator>(new Granted(_grants))
            .AddSingleton<INotifications>(_told)
            .AddSingleton<IKeptRegularEvents>(new KeptInSundays())
            .AddScoped<MinistryViews>()
            .AddScoped<MinistryChanges>()
            .AddScoped<MinistriesPublisher>()
            .AddScoped<MinistriesService>()
            .AddScoped(provider => new MinistryLeadingService(provider.GetRequiredService<MinistriesDbContext>(), provider.GetRequiredService<MinistryViews>(),
                provider.GetRequiredService<MinistryChanges>(), null!, null!, _roles, _people, _people, new NoCalendars(), new KeptInSundays(), _told, null!, _clock))
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
        public ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(Guid ministryId, CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<MinistryCalendar>>([]);

        public ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<MinistryCalendar>>([]);

        public ValueTask ShowAsync(Guid ministryId, bool shown, Audience whileHidden, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<MinistryCalendar> CreateAsync(Guid ministryId, string name, Audience audience, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<MinistryCalendar> ChangeAsync(Guid ministryId, Guid calendarId, string name, Audience audience, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

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
