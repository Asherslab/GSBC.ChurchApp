using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

[Integration("Deleting an account leaves a row with only the name, in one transaction with its passkeys, sessions and codes, and finishes the other features' part later if one fails")]
public sealed class AccountDeletionTests : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private readonly RecordedPersonData _feature = new();
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
            .AddSingleton<IPersonData>(_feature)
            .AddScoped<AccountDeletion>()
            .AddAccountsData()
            .AddSyncData()
            .AddIdentityCore<AccountUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<AccountsDbContext>();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().Database.MigrateAsync(Cancel);
    }

    [Fact]
    public async Task ADeletedAccountKeepsOnlyItsNameCanNotSignInAndItsEmailIsFreeAgain()
    {
        Guid naomi = await NewAccountAsync("naomi@example.com", "Naomi");
        await WithAsync<AccountsDbContext>(async db =>
        {
            db.Set<IdentityUserPasskey<Guid>>().Add(new IdentityUserPasskey<Guid>
            {
                UserId = naomi, CredentialId = naomi.ToByteArray(),
                Data = new IdentityPasskeyData { PublicKey = [1], Transports = [], AttestationObject = [], ClientDataJson = [], CreatedAt = _clock.GetUtcNow() },
            });
            db.Sessions.Add(new AccountSession { Id = Guid.CreateVersion7(), UserId = naomi, Client = SessionClient.App, CreatedAt = _clock.UtcNow, LastSeenAt = _clock.UtcNow, ExpiresAt = _clock.UtcNow.AddDays(1) });
            await db.SaveChangesAsync(Cancel);
        });

        await WithAsync<AccountDeletion>(deletion => deletion.DeleteAsync(naomi, naomi, Cancel));

        AccountUser kept = await WithAsync<AccountsDbContext, AccountUser>(db => db.Users.AsNoTracking().SingleAsync(user => user.Id == naomi, Cancel));
        Assert.Equal(("Naomi", (string?)null, true), (kept.DisplayName, kept.Email, kept.Disabled));
        Assert.NotNull(kept.ForgottenAt);
        Assert.False(await WithAsync<AccountsDbContext, bool>(db => db.Set<IdentityUserPasskey<Guid>>().AnyAsync(passkey => passkey.UserId == naomi, Cancel)));
        Assert.False(await WithAsync<AccountsDbContext, bool>(db => db.Sessions.AnyAsync(session => session.UserId == naomi, Cancel)));
        Assert.Equal([naomi], _feature.Forgotten);
        await NewAccountAsync("naomi@example.com", "Naomi again");
    }

    [Fact]
    public async Task AFeaturesReasonKeepsTheAccountAsItWas()
    {
        Guid owner = await NewAccountAsync("ruth@example.com", "Ruth");
        _feature.Reason = "This account is the church's last Owner. Make someone else an Owner first.";

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => WithAsync<AccountDeletion>(deletion => deletion.DeleteAsync(owner, owner, Cancel)));

        Assert.Equal((StatusCode.FailedPrecondition, _feature.Reason), (refused.StatusCode, refused.Status.Detail));
        Assert.Equal("ruth@example.com", (await WithAsync<AccountsDbContext, AccountUser>(db => db.Users.AsNoTracking().SingleAsync(user => user.Id == owner, Cancel))).Email);
        Assert.Empty(_feature.Forgotten);
    }

    [Fact]
    public async Task AFeatureThatFailsToForgetThemIsAskedAgainByTheCleanUp()
    {
        Guid boaz = await NewAccountAsync("boaz@example.com", "Boaz");
        _feature.FailuresLeft = 1;

        await WithAsync<AccountDeletion>(deletion => deletion.DeleteAsync(boaz, boaz, Cancel));
        Assert.Empty(_feature.Forgotten);
        await WithAsync<AccountDeletion>(deletion => deletion.FinishAsync(Cancel));

        Assert.Equal([boaz], _feature.Forgotten);
        Assert.NotNull((await WithAsync<AccountsDbContext, AccountUser>(db => db.Users.AsNoTracking().SingleAsync(user => user.Id == boaz, Cancel))).ForgottenAt);
    }

    private async Task<Guid> NewAccountAsync(string email, string name)
    {
        Guid id = Guid.CreateVersion7();
        IdentityResult created = await WithAsync<UserManager<AccountUser>, IdentityResult>(users => users.CreateAsync(new AccountUser
        {
            Id = id, UserName = id.ToString(), Email = email, DisplayName = name, CreatedAt = _clock.UtcNow,
        }));
        Assert.True(created.Succeeded);
        return id;
    }

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
}
