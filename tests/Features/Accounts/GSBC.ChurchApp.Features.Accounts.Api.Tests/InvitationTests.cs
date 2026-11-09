using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

[Integration("Invitation links are one-time codes in Postgres: resending replaces the stored code, withdrawing deletes it and the unused account, and a passkey closes the invitation")]
public sealed class InvitationTests : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private readonly SentEmails _sent = new();
    private readonly RecordedPersonData _forgotten = new();
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
            .AddSingleton<IRoleAssignments>(new NoRoles())
            .AddSingleton<IAccessEvaluator>(new NoGrants())
            .AddSingleton(new PermissionCatalog([new PermissionSet(AccountsPermissions.All)]))
            .AddSingleton<IEmailSender>(_sent)
            .AddScoped<DemoAccounts>()
            .AddScoped<Invitations>()
            .AddScoped<AccountDeletion>()
            .AddSingleton<IPersonData>(_forgotten)
            .AddAccountsData()
            .AddSyncData()
            .AddIdentityCore<AccountUser>(options => options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<AccountsDbContext>();
        services.AddOptions<AccountsOptions>();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().Database.MigrateAsync();
        _staff = await NewAccountAsync("staff@example.com", "Ruth");
    }

    [Fact]
    public async Task AnInvitationIsListedWithWhoSentItUntilItIsAccepted()
    {
        Guid invitee = await InviteAsync("naomi@example.com");

        InvitationInfo listed = Assert.Single(await ListAsync());
        Assert.Equal(invitee.ToString(), listed.UserId);
        Assert.Equal("Ruth", listed.InvitedBy);
        Assert.Equal(listed.InvitedAt + OneTimeCodes.InvitationLifetime, listed.ExpiresAt, TimeSpan.FromSeconds(1));

        await GivePasskeyAsync(invitee);
        Assert.Empty(await ListAsync());
    }

    [Fact]
    public async Task ResendingSendsANewLinkAndStopsTheOldOne()
    {
        Guid invitee = await InviteAsync("boaz@example.com");
        string first = _sent.LastLink;

        await WithAsync<Invitations>(invitations => invitations.ResendAsync(invitee.ToString(), _staff, TestContext.Current.CancellationToken));

        Assert.NotEqual(first, _sent.LastLink);
        Assert.Null(await FindAsync(first));
        Assert.NotNull(await FindAsync(_sent.LastLink));
        Assert.Single(await ListAsync());
    }

    [Fact]
    public async Task WithdrawingRemovesTheUnusedAccountAndItsLink()
    {
        Guid invitee = await InviteAsync("orpah@example.com");

        await WithAsync<Invitations>(invitations => invitations.WithdrawAsync(invitee.ToString(), _staff, TestContext.Current.CancellationToken));

        Assert.Empty(await ListAsync());
        Assert.Null(await FindAsync(_sent.LastLink));
        Assert.Equal([invitee], _forgotten.Forgotten);
        Assert.False(await WithAsync<AccountsDbContext, bool>(db => db.Users.AnyAsync(user => user.Id == invitee, TestContext.Current.CancellationToken)));
    }

    [Fact]
    public async Task AnAccountWithAPasskeyIsNeverWithdrawn()
    {
        Guid invitee = await InviteAsync("obed@example.com");
        await GivePasskeyAsync(invitee);

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() =>
            WithAsync<Invitations>(invitations => invitations.WithdrawAsync(invitee.ToString(), _staff, TestContext.Current.CancellationToken)));

        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
        Assert.True(await WithAsync<AccountsDbContext, bool>(db => db.Users.AnyAsync(user => user.Id == invitee, TestContext.Current.CancellationToken)));
    }

    private async Task<Guid> InviteAsync(string email)
    {
        Guid id = await NewAccountAsync(email, email);
        await WithAsync<IServiceProvider>(async provider =>
        {
            AccountUser user = (await provider.GetRequiredService<UserManager<AccountUser>>().FindByIdAsync(id.ToString()))!;
            await provider.GetRequiredService<Invitations>().SendAsync(user, _staff, AuditActions.Invited, TestContext.Current.CancellationToken);
        });
        return id;
    }

    private async Task<List<InvitationInfo>> ListAsync() =>
        (await WithAsync<Invitations, InvitationList>(invitations => invitations.ListAsync(TestContext.Current.CancellationToken))).Invitations;

    private Task<OneTimeCode?> FindAsync(string link) =>
        WithAsync<OneTimeCodes, OneTimeCode?>(codes => codes.FindTokenAsync(link[(link.IndexOf("code=", StringComparison.Ordinal) + 5)..], [CodePurpose.Invitation], TestContext.Current.CancellationToken));

    private async Task<Guid> NewAccountAsync(string email, string name)
    {
        Guid id = Guid.CreateVersion7();
        await WithAsync<UserManager<AccountUser>>(async users => await users.CreateAsync(new AccountUser
        {
            Id = id, UserName = id.ToString(), Email = email, DisplayName = name, CreatedAt = _clock.UtcNow,
        }));
        return id;
    }

    private Task GivePasskeyAsync(Guid userId) => WithAsync<AccountsDbContext>(async db =>
    {
        db.Set<IdentityUserPasskey<Guid>>().Add(new IdentityUserPasskey<Guid>
        {
            UserId = userId,
            CredentialId = userId.ToByteArray(),
            Data = new IdentityPasskeyData { PublicKey = [1], Transports = [], AttestationObject = [], ClientDataJson = [], CreatedAt = _clock.GetUtcNow() },
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    });

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

    private sealed class SentEmails : IEmailSender
    {
        public string LastLink { get; private set; } = "";

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            LastLink = message.Body.Split('\n').Last(line => line.Contains("code=", StringComparison.Ordinal));
            return Task.CompletedTask;
        }
    }
}
