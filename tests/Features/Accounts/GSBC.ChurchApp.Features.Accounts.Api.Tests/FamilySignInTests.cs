using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

[Integration("A child a parent adds is an account row with no email or passkey that the abandoned-account clean-up keeps; its sign-in code is a one-time code in Postgres, spent once a passkey is made with it, however it's typed")]
public sealed class FamilySignInTests : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private ServiceProvider _services = null!;
    private readonly Guid _parent = Guid.CreateVersion7();

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
            .AddSingleton<IRoleAssignments>(new NoRoles())
            .AddSingleton<IAccessEvaluator>(new NoGrants())
            .AddSingleton(new PermissionCatalog([new PermissionSet(AccountsPermissions.All)]))
            .AddSingleton<IEmailSender>(new NoEmail())
            .AddScoped<DemoAccounts>()
            .AddScoped<ReviewSignIns>()
            .AddScoped<Invitations>()
            .AddScoped<AccountDeletion>()
            .AddScoped<AccountsService>()
            .AddScoped<IPeopleAccounts, AccountsPeopleAccounts>()
            .AddAccountsData()
            .AddSyncData()
            .AddAccountIdentity();
        services.AddOptions<AccountsOptions>();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().Database.MigrateAsync(Cancel);
    }

    [Fact]
    public async Task AParentsCodeRedeemsForTheirChildWhateverItsCaseOrSpacing()
    {
        Guid mia = await WithAsync<IPeopleAccounts, Guid>(accounts => accounts.AddWithoutSignInAsync("Mia", _parent, Cancel).AsTask());
        FamilySignInCode code = await WithAsync<IPeopleAccounts, FamilySignInCode>(accounts => accounts.MakeSignInCodeAsync(mia, _parent, Cancel).AsTask());

        TicketReply ticket = await RedeemAsync($" {code.Code.ToLowerInvariant().Replace("-", " ", StringComparison.Ordinal)} ");

        OneTimeCode issued = await WithAsync<AccountsDbContext, OneTimeCode>(db =>
            db.Codes.AsNoTracking().SingleAsync(found => found.Purpose == CodePurpose.RegisterPasskey, Cancel));
        Assert.False(string.IsNullOrEmpty(ticket.Ticket));
        Assert.Equal(mia, issued.UserId);
    }

    [Fact]
    public async Task ACodeIsSpentOnlyWhenAPasskeyIsMadeWithIt()
    {
        Guid mia = await WithAsync<IPeopleAccounts, Guid>(accounts => accounts.AddWithoutSignInAsync("Mia", _parent, Cancel).AsTask());
        FamilySignInCode code = await WithAsync<IPeopleAccounts, FamilySignInCode>(accounts => accounts.MakeSignInCodeAsync(mia, _parent, Cancel).AsTask());

        TicketReply cancelled = await RedeemAsync(code.Code);
        TicketReply retried = await RedeemAsync(code.Code);
        bool madePasskey = await SpendAsync(retried);
        bool spentAgain = await SpendAsync(cancelled);
        RpcException afterwards = await Assert.ThrowsAsync<RpcException>(() => RedeemAsync(code.Code));

        Assert.True(madePasskey);
        Assert.False(spentAgain);
        Assert.Equal(StatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task AChildWithNoEmailCanBeSavedWhenTheirPasskeyIsAdded()
    {
        Guid mia = await WithAsync<IPeopleAccounts, Guid>(accounts => accounts.AddWithoutSignInAsync("Mia", _parent, Cancel).AsTask());

        IdentityResult saved = await WithAsync<UserManager<AccountUser>, IdentityResult>(async users =>
            await users.UpdateAsync(await users.FindByIdAsync(mia.ToString()) ?? throw new InvalidOperationException("No child")));

        Assert.True(saved.Succeeded, string.Join(" ", saved.Errors.Select(error => error.Description)));
    }

    [Fact]
    public async Task AMadeCodeReplacesTheOneBefore()
    {
        Guid leo = await WithAsync<IPeopleAccounts, Guid>(accounts => accounts.AddWithoutSignInAsync("Leo", _parent, Cancel).AsTask());
        FamilySignInCode first = await WithAsync<IPeopleAccounts, FamilySignInCode>(accounts => accounts.MakeSignInCodeAsync(leo, _parent, Cancel).AsTask());
        await WithAsync<IPeopleAccounts, FamilySignInCode>(accounts => accounts.MakeSignInCodeAsync(leo, _parent, Cancel).AsTask());

        Assert.Equal(StatusCode.NotFound, (await Assert.ThrowsAsync<RpcException>(() => RedeemAsync(first.Code))).StatusCode);
    }

    [Fact]
    public async Task TheCleanUpKeepsAChildWhoHasNoPasskeyYet()
    {
        Guid mia = await WithAsync<IPeopleAccounts, Guid>(accounts => accounts.AddWithoutSignInAsync("Mia", _parent, Cancel).AsTask());

        DateTime muchLater = _clock.UtcNow + AccountsCleanup.AbandonedAfter + TimeSpan.FromDays(30);
        await WithAsync<AccountsDbContext, bool>(async db => { await AccountsCleanup.CleanAsync(db, muchLater, [], Cancel); return true; });

        Assert.True(await WithAsync<AccountsDbContext, bool>(db => db.Users.AnyAsync(user => user.Id == mia, Cancel)));
    }

    private Task<bool> SpendAsync(TicketReply reply) =>
        WithAsync<OneTimeCodes, bool>(async codes =>
        {
            OneTimeCode ticket = await codes.FindTokenAsync(reply.Ticket, [CodePurpose.RegisterPasskey], Cancel) ?? throw new InvalidOperationException("No ticket");
            return await codes.SpendTicketAsync(ticket, Cancel);
        });

    private Task<TicketReply> RedeemAsync(string code) =>
        WithAsync<AccountsService, TicketReply>(service => service.RedeemAsync(new RedeemRequest { Code = code }).AsTask());

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

    private sealed class NoEmail : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
