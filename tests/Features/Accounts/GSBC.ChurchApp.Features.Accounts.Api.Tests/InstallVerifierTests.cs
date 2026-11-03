using System.Text;
using GSBC.ChurchApp.Features.Accounts.Api.Installs;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Api.Installs;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Accounts.Api.Tests;

public sealed class InstallVerifierTests : IAsyncLifetime
{
    private const string AppId = "YNT9A4LGVA.au.com.baptist.app.dev";
    private static readonly byte[] Request = Encoding.UTF8.GetBytes("/gsbc.notifications.Notifications/RegisterDevice\nbody");

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private readonly TestDevice _device = new(AppId);
    private ServiceProvider _services = null!;
    private Guid _install;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())])
                .Build())
            .AddSingleton(TimeProvider.System)
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
            AttestedAt = DateTime.UtcNow, LastSeenAt = DateTime.UtcNow,
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
        InstallVerifier verifier = new(scope.ServiceProvider.GetRequiredService<AccountsDbContext>(), TimeProvider.System);
        return await verifier.VerifyAsync(proof, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _device.Dispose();
        await _services.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
