using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

[Integration("Sign-in codes are matched in Postgres: each use stores a session that turning the code off deletes, wrong guesses are counted from audit rows, and cleanup spares demo accounts")]
public sealed class ReviewSignInTests : IAsyncLifetime
{
    private const string Address = "203.0.113.7";

    private TestDatabase _database = null!;

    private readonly TestClock _clock = new();
    private readonly RoleHolders _roles = new();
    private ServiceProvider _services = null!;
    private Guid _staff;

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
            .AddSingleton<IAccessEvaluator>(new NoGrants())
            .AddSingleton(new PermissionCatalog([new PermissionSet(AccountsPermissions.All)]))
            .AddScoped<DemoAccounts>()
            .AddScoped<ReviewSignIns>()
            .AddAccountsData()
            .AddSyncData()
            .AddIdentityCore<AccountUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<AccountsDbContext>();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().Database.MigrateAsync();
        _staff = await NewAccountAsync("staff@example.com");
    }

    [Fact]
    public async Task EachUseStoresASessionThatEndsWhenItsCodeIsTurnedOff()
    {
        Guid demo = await NewDemoAccountAsync("review@example.com");
        string code = (await CreateCodeAsync(demo)).Code;

        string secret = await SignInAsync("review@example.com", code);
        AccountSession session = await SessionAsync(secret);
        Assert.Equal(demo, session.UserId);
        Assert.NotNull(session.ReviewCodeId);

        await WithAsync<ReviewSignIns>(reviews => reviews.TurnOffAsync(_staff, session.ReviewCodeId.ToString()!, TestContext.Current.CancellationToken));
        Assert.False(await SessionExistsAsync(secret));
        Assert.Equal(StatusCode.Unauthenticated, await RefusalAsync("review@example.com", code));
    }

    [Fact]
    public async Task ACodeOnlyOpensTheAccountItWasMadeFor()
    {
        Guid demo = await NewDemoAccountAsync("demo@example.com");
        string code = (await CreateCodeAsync(demo)).Code;
        await NewDemoAccountAsync("person@example.com");

        Assert.Equal(StatusCode.Unauthenticated, await RefusalAsync("person@example.com", code));
        Assert.Equal(0, await WithDbAsync(db => db.Sessions.CountAsync(TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task WrongGuessesAreThrottledPerAccountAndPerAddress()
    {
        Guid demo = await NewDemoAccountAsync("guess@example.com");
        string code = (await CreateCodeAsync(demo)).Code;

        for (int guess = 0; guess < ReviewSignIns.WrongGuessesPerAccount; guess++)
            Assert.Equal(StatusCode.Unauthenticated, await RefusalAsync("guess@example.com", "AAAA-AAAA-AAAA"));
        Assert.Equal(StatusCode.ResourceExhausted, await RefusalAsync("guess@example.com", code, "198.51.100.1"));

        for (int guess = 0; guess < ReviewSignIns.WrongGuessesPerAddress; guess++)
            await RefusalAsync($"nobody{guess}@example.com", "AAAA-AAAA-AAAA", "198.51.100.2");
        Guid other = await NewDemoAccountAsync("other@example.com");
        string otherCode = (await CreateCodeAsync(other)).Code;
        Assert.Equal(StatusCode.ResourceExhausted, await RefusalAsync("other@example.com", otherCode, "198.51.100.2"));
        Assert.NotEmpty(await SignInAsync("other@example.com", otherCode, "198.51.100.3"));
    }

    [Fact]
    public async Task DemoAccountsAreNeverCleanedUpAsAbandonedSignUps()
    {
        Guid demo = await NewDemoAccountAsync("kept@example.com");
        Guid abandoned = await NewAccountAsync("gone@example.com");
        DateTime later = _clock.UtcNow + AccountsCleanup.AbandonedAfter + TimeSpan.FromDays(1);

        await WithAsync<AccountsDbContext>(db => AccountsCleanup.CleanAsync(db, later, [demo], TestContext.Current.CancellationToken));

        Assert.True(await WithDbAsync(db => db.Users.AnyAsync(user => user.Id == demo, TestContext.Current.CancellationToken)));
        Assert.False(await WithDbAsync(db => db.Users.AnyAsync(user => user.Id == abandoned, TestContext.Current.CancellationToken)));
    }

    private Task<CodeReply> CreateCodeAsync(Guid demo) =>
        WithAsync<ReviewSignIns, CodeReply>(reviews => reviews.CreateCodeAsync(_staff, demo.ToString(), "TestFlight", 0, TestContext.Current.CancellationToken));

    private Task<string> SignInAsync(string email, string code, string address = Address) =>
        WithAsync<ReviewSignIns, string>(reviews => reviews.SignInAsync(email, code, "iPhone", address, TestContext.Current.CancellationToken));

    private async Task<StatusCode> RefusalAsync(string email, string code, string address = Address) =>
        (await Assert.ThrowsAsync<RpcException>(() => SignInAsync(email, code, address))).StatusCode;

    private async Task<Guid> NewDemoAccountAsync(string email)
    {
        Guid id = await NewAccountAsync(email);
        _roles.Give(id);
        return id;
    }

    private async Task<Guid> NewAccountAsync(string email)
    {
        Guid id = Guid.CreateVersion7();
        await WithAsync<UserManager<AccountUser>>(async users => await users.CreateAsync(new AccountUser
        {
            Id = id, UserName = id.ToString(), Email = email, DisplayName = email, CreatedAt = _clock.UtcNow,
        }));
        return id;
    }

    private Task<AccountSession> SessionAsync(string secret) =>
        WithDbAsync(db => db.Sessions.SingleAsync(session => session.SecretHash == AppSessions.HashOf(secret), TestContext.Current.CancellationToken));

    private Task<bool> SessionExistsAsync(string secret) =>
        WithDbAsync(db => db.Sessions.AnyAsync(session => session.SecretHash == AppSessions.HashOf(secret), TestContext.Current.CancellationToken));

    private Task<T> WithDbAsync<T>(Func<AccountsDbContext, Task<T>> action) => WithAsync(action);

    private async Task WithAsync<TService>(Func<TService, Task> action) where TService : notnull =>
        await WithAsync<TService, bool>(async service => { await action(service); return true; });

    private async Task<T> WithAsync<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    private sealed class RoleHolders : IRoleAssignments
    {
        private readonly HashSet<Guid> _holders = [];

        public void Give(Guid userId) => _holders.Add(userId);

        public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<RoleHolder>>(roleKey == DemoAccounts.RoleKey ? _holders.Select(id => new RoleHolder(id)).ToList() : []);

        public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken)
        {
            Give(userId);
            return ValueTask.CompletedTask;
        }

        public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class NoGrants : IAccessEvaluator
    {
        public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);

        public ValueTask<bool> IsAllowedAsync(System.Security.Claims.ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
