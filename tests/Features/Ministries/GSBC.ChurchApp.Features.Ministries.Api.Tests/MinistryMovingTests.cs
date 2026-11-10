using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

[Integration("Moving a ministry re-parents it in Postgres, which Access's hierarchy reads for inherited roles, and tells Access and the feeds that the tree changed")]
public sealed class MinistryMovingTests : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private readonly LeadingRoles _roles = new();
    private readonly NamedPeople _people = new();
    private ServiceProvider _services = null!;

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        ServiceCollection services = new();
        services
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
                .Build())
            .AddSingleton<TimeProvider>(_clock)
            .AddLogging()
            .AddSingleton<IRoleAssignments>(_roles)
            .AddSingleton<IPeopleIndex>(_people)
            .AddScoped<MinistryChanges>()
            .AddScoped<MinistryMover>()
            .AddSingleton<MinistryHierarchy>()
            .AddMinistriesData()
            .AddSyncData();
        _services = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<MinistriesDbContext>().Database.MigrateAsync(Cancel);
    }

    [Fact]
    public async Task MovingAMinistryChangesWhoLeadsItThroughTheTreeAndNothingOfItsOwn()
    {
        MinistryRecord music = Ministry("Music", null), choir = Ministry("Choir", music.Id), altos = Ministry("Altos", choir.Id), kids = Ministry("Kids Ministries", null);
        Guid sam = _people.Add("Sam"), kim = _people.Add("Kim"), cleo = _people.Add("Cleo");
        _roles.Lead(sam, music.Id);
        _roles.Lead(kim, kids.Id);
        _roles.Lead(cleo, choir.Id);
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
            db.Ministries.AddRange(music, choir, altos, kids);
            await db.SaveChangesAsync(Cancel);
        }

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            MinistryTree tree = new(await scope.ServiceProvider.GetRequiredService<MinistriesDbContext>().Ministries.AsNoTracking().ToListAsync(Cancel));
            MinistryMover mover = scope.ServiceProvider.GetRequiredService<MinistryMover>();
            MovePreview preview = await mover.PreviewAsync(tree, tree.Find(choir.Id)!, tree.Find(kids.Id), Cancel);
            Assert.Equal(("Music", "Kids Ministries", 1), (preview.From, preview.To, preview.SubMinistries));
            Assert.Equal([("Music", "Sam")], preview.Lose.SelectMany(change => change.People.Select(person => (change.Ministry, person))));
            Assert.Equal([("Kids Ministries", "Kim")], preview.Gain.SelectMany(change => change.People.Select(person => (change.Ministry, person))));
            await mover.MoveAsync(tree, tree.Find(choir.Id)!, tree.Find(kids.Id), Cancel);
        }

        MinistryHierarchy hierarchy = _services.GetRequiredService<MinistryHierarchy>();
        Assert.Equal(kids.Id.ToString(), await hierarchy.ParentOfAsync(ScopeKind.Ministry, choir.Id.ToString(), Cancel));
        Assert.Empty(await hierarchy.ChildrenOfAsync(ScopeKind.Ministry, music.Id.ToString(), Cancel));
        Assert.Equal(choir.Id.ToString(), await hierarchy.ParentOfAsync(ScopeKind.Ministry, altos.Id.ToString(), Cancel));
        Assert.Equal(1, _roles.ScopesChanged);

        await using AsyncServiceScope check = _services.CreateAsyncScope();
        HashSet<string> touched = [.. await check.ServiceProvider.GetRequiredService<MinistriesDbContext>().FeedChanges
            .Where(change => change.Feed == MinistriesFeeds.Ministries).Select(change => change.RecordId).ToListAsync(Cancel)];
        Assert.Subset(touched, new HashSet<string>([choir.Id.ToString(), altos.Id.ToString(), music.Id.ToString(), kids.Id.ToString()]));
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    private MinistryRecord Ministry(string name, Guid? parent) => new()
    {
        Id = Guid.CreateVersion7(), ParentId = parent, Name = name, Kind = MinistryKinds.Ministry, TeamJoin = Joining.Ask, AttendJoin = Joining.None,
        CreatedAt = _clock.UtcNow, UpdatedAt = _clock.UtcNow,
    };

    private sealed class NamedPeople : IPeopleIndex
    {
        private readonly Dictionary<Guid, string> _names = [];

        public Guid Add(string name)
        {
            Guid id = Guid.CreateVersion7();
            _names[id] = name;
            return id;
        }

        public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyDictionary<Guid, string>>(people.Where(_names.ContainsKey).ToDictionary(person => person, person => _names[person]));

        public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<IReadOnlySet<Guid>> NotInvitedYetAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class LeadingRoles : IRoleAssignments
    {
        private readonly List<(Guid Person, string Scope)> _leaders = [];

        public int ScopesChanged { get; private set; }

        public void Lead(Guid person, Guid ministry) => _leaders.Add((person, ministry.ToString()));

        public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<RoleHolder>>(roleKey == MinistriesPermissions.LeaderRole
                ? _leaders.Where(leader => leader.Scope == objectId).Select(leader => new RoleHolder(leader.Person)).ToList()
                : []);

        public ValueTask ScopesChangedAsync(CancellationToken cancellationToken)
        {
            ScopesChanged++;
            return ValueTask.CompletedTask;
        }

        public ValueTask ForgetScopeAsync(ScopeKind kind, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<int> HoldingCountAsync(string roleKey, string objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
