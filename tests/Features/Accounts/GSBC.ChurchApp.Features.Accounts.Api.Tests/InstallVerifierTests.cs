using GSBC.ChurchApp.Features.Accounts.Api.Installs;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Installs;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

[Integration("An install's assertion counter only moves forward through one conditional UPDATE, so a replayed or overtaken assertion is refused")]
public sealed class InstallVerifierTests : IAsyncLifetime
{
    private const string AppId = "YNT9A4LGVA.au.com.baptist.app.dev";
    private static readonly byte[] Request = "/gsbc.notifications.Notifications/RegisterDevice\nbody"u8.ToArray();

    private TestDatabase _database = null!;

    private readonly TestClock _clock = new();
    private readonly TestDevice _device = new(AppId);
    private ServiceProvider _services = null!;
    private Guid _install;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
                .Build())
            .AddSingleton<TimeProvider>(_clock)
            .AddLogging()
            .AddAccountsData()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        AccountsDbContext db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();
        await db.Database.MigrateAsync();
        _install = Guid.CreateVersion7();
        db.Installs.Add(new AppInstall
        {
            Id = _install, KeyId = _device.KeyId, PublicKey = _device.Key.ExportSubjectPublicKeyInfo(), AppId = AppId,
            AttestedAt = _clock.UtcNow, LastSeenAt = _clock.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task AnAssertionIsRefusedWhenReplayedOrWhenALaterOneArrivedFirst()
    {
        InstallProof third = Proof(counter: 3);

        Assert.Equal(_install, await VerifyAsync(third));
        Assert.Null(await VerifyAsync(third));
        Assert.Null(await VerifyAsync(Proof(counter: 2)));
        Assert.Equal(_install, await VerifyAsync(Proof(counter: 4)));
    }

    private InstallProof Proof(uint counter) => new(_device.KeyId, _device.Assertion(Request, counter), Request);

    private async Task<Guid?> VerifyAsync(InstallProof proof)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        InstallVerifier verifier = new(scope.ServiceProvider.GetRequiredService<AccountsDbContext>(), _clock);
        return await verifier.VerifyAsync(proof, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _device.Dispose();
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
