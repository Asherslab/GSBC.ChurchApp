using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

[Integration("Deleting a ministry moves its teams up to its parent and removes its posts, files and asks in one transaction, then forgets its roles and calendars")]
public sealed class MinistryRemovalTests : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private readonly TestClock _clock = new();
    private readonly ForgettingRoles _roles = new();
    private readonly OwnedCalendars _calendars = new();
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
            .AddSingleton<IMinistryCalendars>(_calendars)
            .AddScoped<MinistryChanges>()
            .AddScoped<MinistryRemover>()
            .AddMinistriesData()
            .AddSyncData();
        _services = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync(Cancel);
        await scope.ServiceProvider.GetRequiredService<MinistriesDbContext>().Database.MigrateAsync(Cancel);
    }

    [Fact]
    public async Task AMinistrysTeamsMoveUpAndEverythingElseOfItsGoes()
    {
        MinistryRecord music = Ministry("Music", null), choir = Ministry("Choir", music.Id), altos = Ministry("Altos", choir.Id), youth = Ministry("Impact Youth", null);
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
            db.Ministries.AddRange(music, choir, altos, youth);
            db.Posts.Add(new PostRecord { Id = Guid.CreateVersion7(), MinistryId = choir.Id, Title = "Rehearsal moved", Audience = Audiences.Team, PostedAt = _clock.UtcNow });
            db.Posts.Add(new PostRecord { Id = Guid.CreateVersion7(), MinistryId = youth.Id, Title = "Camp", Audience = Audiences.Everyone, PostedAt = _clock.UtcNow });
            db.Files.Add(new FileRecord { Id = Guid.CreateVersion7(), MinistryId = choir.Id, Title = "Score", Kind = FileKinds.Link, Url = "https://example.com", Audience = Audiences.Team });
            db.Asks.Add(new AskRecord { Id = Guid.CreateVersion7(), MinistryId = choir.Id, PersonId = Guid.CreateVersion7(), As = Places.Team, AskedAt = _clock.UtcNow });
            await db.SaveChangesAsync(Cancel);
        }
        _calendars.Owned[choir.Id] = [Guid.CreateVersion7()];

        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
            MinistryTree tree = new(await db.Ministries.AsNoTracking().ToListAsync(Cancel));
            await scope.ServiceProvider.GetRequiredService<MinistryRemover>().RemoveAsync(tree, tree.Find(choir.Id)!, Cancel);
        }

        await using AsyncServiceScope check = _services.CreateAsyncScope();
        MinistriesDbContext after = check.ServiceProvider.GetRequiredService<MinistriesDbContext>();
        Assert.Equal(music.Id, (await after.Ministries.SingleAsync(ministry => ministry.Id == altos.Id, Cancel)).ParentId);
        Assert.False(await after.Ministries.AnyAsync(ministry => ministry.Id == choir.Id, Cancel));
        Assert.Equal(["Camp"], await after.Posts.Select(post => post.Title).ToListAsync(Cancel));
        Assert.False(await after.Files.AnyAsync(Cancel));
        Assert.False(await after.Asks.AnyAsync(Cancel));
        Assert.Equal([choir.Id.ToString()], _roles.Forgotten);
        Assert.Empty(_calendars.Owned[choir.Id]);
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

    private sealed class ForgettingRoles : IRoleAssignments
    {
        public List<string> Forgotten { get; } = [];

        public ValueTask ForgetScopeAsync(ScopeKind kind, IReadOnlyCollection<string> objectIds, CancellationToken cancellationToken)
        {
            Forgotten.AddRange(objectIds);
            return ValueTask.CompletedTask;
        }

        public ValueTask ScopesChangedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

        public ValueTask<int> HoldingCountAsync(string roleKey, string objectId, CancellationToken cancellationToken) => ValueTask.FromResult(0);

        public ValueTask<AssignmentOutcome> GiveAsync(Guid actorId, Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<AssignmentOutcome> TakeAwayAsync(Guid actorId, Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<bool> MayGiveAsync(Guid actorId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask GiveAsFeatureAsync(Guid userId, string roleKey, string? objectId, DateTime? expiresAt, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask TakeAwayAsFeatureAsync(Guid userId, string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<IReadOnlyList<RoleHolder>> HoldersAsync(string roleKey, string? objectId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask MoveScopeAsync(ScopeKind kind, string fromObjectId, string toObjectId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class OwnedCalendars : IMinistryCalendars
    {
        public Dictionary<Guid, List<Guid>> Owned { get; } = [];

        public ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(Guid ministryId, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<MinistryCalendar>>(Owned.GetValueOrDefault(ministryId, []).Select(id => new MinistryCalendar(id, ministryId, "Choir", "choir", Audience.Everyone, false)).ToList());

        public ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask ShowAsync(Guid ministryId, bool shown, Audience whileHidden, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask RemoveAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken)
        {
            Owned[ministryId].Remove(calendarId);
            return ValueTask.CompletedTask;
        }

        public ValueTask<MinistryCalendar> CreateAsync(Guid ministryId, string name, Audience audience, CancellationToken cancellationToken) => throw new NotSupportedException();

        public ValueTask<MinistryCalendar> ChangeAsync(Guid ministryId, Guid calendarId, string name, Audience audience, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValueTask<MinistryCalendarRemoval> WhatGoesAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
