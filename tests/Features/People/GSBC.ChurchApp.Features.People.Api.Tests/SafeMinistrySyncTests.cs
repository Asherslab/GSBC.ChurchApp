using System.Net;
using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Api.SafeMinistry;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

[Integration("A sync keeps only linked people's blue card and training, a link outlives a changed email, and a record staff unlinked waits to be linked by hand")]
public sealed class SafeMinistrySyncTests : IAsyncLifetime
{
    private static readonly DateOnly AlexBorn = new(1990, 5, 15);
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private readonly FakeIndex _index = new();
    private readonly FakeSmc _smc;
    private ServiceProvider _services = null!;
    private FakeAccounts _accounts = null!;

    public SafeMinistrySyncTests() => _smc = new FakeSmc(_clock);

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
            .AddSingleton<IRoleAssignments>(new RecordedRoles())
            .AddSingleton<INotifications>(new QuietNotifications())
            .AddSingleton<IPeopleAccounts>(provider => new FakeAccounts(provider))
            .AddSingleton<IPeopleIndex>(_index)
            .AddSingleton(Options.Create(new SafeMinistryOptions { Subdomain = "example-church", ApiKey = "test-key" }))
            .AddSingleton(new SmcGate())
            .AddSingleton(new HttpClient(_smc))
            .AddScoped<SafeMinistryClient>()
            .AddScoped<SafeMinistrySync>()
            .AddScoped<Families>()
            .AddPeopleData()
            .AddSyncData();
        _services = services.BuildServiceProvider();
        _accounts = (FakeAccounts)_services.GetRequiredService<IPeopleAccounts>();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<PeopleDbContext>().Database.MigrateAsync(Cancel);
    }

    [Fact]
    public async Task ASyncLinksByEmailOrNameAndBirthDateAndKeepsTheLink()
    {
        (Guid sam, Guid alex) = await TwoPeopleAsync();
        _smc.Everyone = [Sam("SAM@example.com", police: "pending"), Alex, Pat];

        await SyncAsync();
        _smc.Everyone = [Sam("sam.new@example.com", police: "cleared"), Alex, Pat];
        await SyncAsync();

        await WithAsync<PeopleDbContext>(async db =>
        {
            Dictionary<Guid, SafeguardingEntry> entries = await db.Safeguarding.ToDictionaryAsync(entry => entry.UserId, Cancel);
            Assert.Equal(("1", SmcLinkHow.Email, "123456/1", new DateOnly(2028, 1, 31)),
                (entries[sam].ExternalId, entries[sam].LinkedHow, entries[sam].CardNumber, entries[sam].CardExpires));
            Assert.Empty(entries[sam].CheckReasons);
            Assert.Equal(("2", SmcLinkHow.NameAndBirthDate), (entries[alex].ExternalId, entries[alex].LinkedHow));
            Assert.Equal(SmcWhy.NoOne, (await db.SmcPeople.SingleAsync(person => person.SmcId == "3", Cancel)).Why);
            SmcSyncState state = await db.SmcSync.SingleAsync(Cancel);
            Assert.Equal((3, 2, 1, 0, null), (state.People, state.Linked, state.ToMatch, state.ToCheck, state.Problem));
        });
    }

    [Fact]
    public async Task AnUnlinkedRecordIsNotMatchedAgainUntilStaffLinkItByHand()
    {
        (Guid sam, _) = await TwoPeopleAsync();
        _smc.Everyone = [Sam("sam@example.com", police: "pending")];
        await SyncAsync();

        await WithAsync<SafeMinistrySync>(sync => sync.UnlinkAsync(sam, Cancel));
        await SyncAsync();
        await WithAsync<PeopleDbContext>(async db =>
        {
            Assert.False(await db.Safeguarding.AnyAsync(Cancel));
            Assert.Equal(SmcWhy.ByHandOnly, (await db.SmcPeople.SingleAsync(Cancel)).Why);
        });

        await WithAsync<SafeMinistrySync>(sync => sync.LinkByHandAsync(sam, "1", sam, Cancel));
        await WithAsync<PeopleDbContext>(async db =>
        {
            SafeguardingEntry entry = await db.Safeguarding.SingleAsync(Cancel);
            Assert.Equal((sam, SmcLinkHow.ByHand, (Guid?)sam, "123456/1"), (entry.UserId, entry.LinkedHow, entry.LinkedBy, entry.CardNumber));
            Assert.Equal([SmcReasons.PoliceCheck], entry.CheckReasons);
        });
    }

    [Fact]
    public async Task AFailedSyncChangesNothingAndSaysWhy()
    {
        (Guid sam, _) = await TwoPeopleAsync();
        _smc.Everyone = [Sam("sam@example.com")];
        await SyncAsync();

        _smc.Refuse = true;
        await SyncAsync();

        await WithAsync<PeopleDbContext>(async db =>
        {
            Assert.Equal(sam, (await db.Safeguarding.SingleAsync(Cancel)).UserId);
            Assert.Equal("Safe Ministry Check didn't accept the app's API key.", (await db.SmcSync.SingleAsync(Cancel)).Problem);
        });
    }

    private static string Sam(string email, string police = "") =>
        SmcSamples.Person("1", "Sam", "Example", email: email, wwccId: "123456/1", wwccExpiry: "2028-01-31", progress: "complete", nextAction: "2027-05-14", police: police);

    private static string Alex => SmcSamples.Person("2", "Alex", "Example", middle: "Jo", dob: "1990-05-15");

    private static string Pat => SmcSamples.Person("3", "Pat", "Nobody", email: "pat@example.com");

    private async Task<(Guid Sam, Guid Alex)> TwoPeopleAsync()
    {
        Guid sam = _accounts.Add("Sam Example", canSignIn: true);
        Guid alex = _accounts.Add("Alex Example", canSignIn: false, addedByFamily: true);
        _index.People.Add(new IndexedPerson(sam, "Sam Example", "sam@example.com", Disabled: false));
        _index.People.Add(new IndexedPerson(alex, "Alex Example", "", Disabled: false));
        await WithAsync<PeopleDbContext>(async db =>
        {
            db.People.Add(new PersonRecord { UserId = alex, BirthDate = AlexBorn, UpdatedAt = _clock.UtcNow });
            await db.SaveChangesAsync(Cancel);
        });
        return (sam, alex);
    }

    private Task SyncAsync() => WithAsync<SafeMinistrySync>(async sync => Assert.True(await sync.RunAsync(Cancel)));

    private async Task WithAsync<TService>(Func<TService, Task> action) where TService : notnull
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    private sealed class FakeIndex : IPeopleIndex
    {
        public List<IndexedPerson> People { get; } = [];

        public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<Guid>>(People.Select(person => person.Id).ToList());

        public ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<IndexedPerson>>(People.Where(person => ids.Contains(person.Id)).ToList());

        public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeSmc(TestClock clock) : HttpMessageHandler
    {
        public IReadOnlyList<string> Everyone { get; set; } = [];
        public bool Refuse { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            if (Refuse)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            string path = request.RequestUri!.AbsolutePath;
            string body = path.EndsWith("/people", StringComparison.Ordinal)
                ? SmcSamples.Page(1, 1, [.. Everyone])
                : Everyone.Single(person => person.Contains($"\"smc_id\": \"{path[(path.LastIndexOf('/') + 1)..]}\"", StringComparison.Ordinal));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }
    }
}
