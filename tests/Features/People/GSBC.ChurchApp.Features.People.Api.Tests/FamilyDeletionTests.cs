using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

[Integration("Deleting a parent removes, through Accounts, each child who can't sign in and has no other parent, and the people schema forgets every link, ask, birth date and blue card of whoever goes")]
public sealed class FamilyDeletionTests : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private readonly RecordedRoles _roles = new();
    private ServiceProvider _services = null!;
    private FakeAccounts _accounts = null!;

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
            .AddSingleton<IAccountEmails>(new NoEmails())
            .AddSingleton<INotifications>(new QuietNotifications())
            .AddSingleton<IPeopleAccounts>(provider => _accounts = new FakeAccounts(provider))
            .AddScoped<Families>()
            .AddScoped<IPersonData, PeoplePersonData>()
            .AddPeopleData()
            .AddSyncData();
        _services = services.BuildServiceProvider();
        _accounts = (FakeAccounts)_services.GetRequiredService<IPeopleAccounts>();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<PeopleDbContext>().Database.MigrateAsync(Cancel);
    }

    [Fact]
    public async Task AChildLeftWithNoParentAndNoSignInGoesWithTheirParent()
    {
        Guid ruth = _accounts.Add("Ruth", canSignIn: true);
        Guid naomi = _accounts.Add("Naomi", canSignIn: true);
        Guid mia = _accounts.Add("Mia", canSignIn: false, addedByFamily: true);
        Guid leo = _accounts.Add("Leo", canSignIn: true, addedByFamily: true);
        Guid sam = _accounts.Add("Sam", canSignIn: false, addedByFamily: true);
        await LinkAsync(ruth, mia, born: new DateOnly(2018, 3, 1));
        await LinkAsync(ruth, leo, born: new DateOnly(2013, 2, 2));
        await LinkAsync(ruth, sam, born: new DateOnly(2016, 7, 9));
        await LinkAsync(naomi, sam, born: null);
        await WithAsync<PeopleDbContext>(async db =>
        {
            db.Safeguarding.Add(new SafeguardingEntry { UserId = ruth, CardNumber = "1234567/2", Source = SafeguardingSources.ByHand });
            db.Asks.Add(new LinkAskRecord { Id = Guid.CreateVersion7(), ChildId = leo, ParentEmail = "DAD@EXAMPLE.COM", AskedBy = ruth, AskedAt = _clock.UtcNow });
            await db.SaveChangesAsync(Cancel);
        });

        await WithAsync<IPersonData>(feature => feature.ForgetAsync(ruth, Cancel));

        Assert.Equal([mia], _accounts.Removed);
        await WithAsync<PeopleDbContext>(async db =>
        {
            Assert.Equal([(naomi, sam)], await db.Links.Select(link => new ValueTuple<Guid, Guid>(link.ParentId, link.ChildId)).ToListAsync(Cancel));
            Assert.False(await db.People.AnyAsync(person => person.UserId == mia, Cancel));
            Assert.False(await db.Safeguarding.AnyAsync(Cancel));
            Assert.False(await db.Asks.AnyAsync(Cancel));
        });
        Assert.Contains((naomi, sam.ToString()), _roles.Given.Select(given => (given.UserId, given.ObjectId)));
        Assert.Equal([mia.ToString(), ruth.ToString()], _roles.ForgottenPlaces);
    }

    private async Task LinkAsync(Guid parent, Guid child, DateOnly? born)
    {
        await WithAsync<PeopleDbContext>(async db =>
        {
            if (born is not null && !await db.People.AnyAsync(person => person.UserId == child, Cancel))
            {
                db.People.Add(new PersonRecord { UserId = child, BirthDate = born, UpdatedAt = _clock.UtcNow });
                await db.SaveChangesAsync(Cancel);
            }
        });
        await WithAsync<Families>(families => families.LinkAsync(parent, child, LinkHow.Added, parent, Cancel));
    }

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
}
