using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public abstract class EventsHost : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    internal TestClock Clock { get; } = new(ChurchTime.ToUtc(new DateTime(2026, 11, 8, 20, 0, 0)));
    internal Grants Access { get; } = new();
    internal RecordedNotifications Notifications { get; } = new();
    internal RecordedRoles Roles { get; } = new();

    protected static CancellationToken Cancel => TestContext.Current.CancellationToken;

    protected static DateOnly Today => new(2026, 11, 8);

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
            .Build());
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IAccessEvaluator>(Access);
        services.AddSingleton<INotifications>(Notifications);
        services.AddSingleton<IMediaReferences, AnyMedia>();
        services.AddScoped<EventsPublisher>();
        services.AddScoped<EventWriter>();
        services.AddScoped<HandledRequests>();
        services.AddScoped<EventEditingService>();
        services.AddScoped<CalendarSetupService>();
        services.AddScoped<CalendarFollowing>();
        services.AddScoped<CalendarRemover>();
        services.AddSingleton<IRoleAssignments>(Roles);
        services.AddSingleton<IPhoneRoles>(Roles);
        services.AddScoped<CalendarLinkSecrets>();
        services.AddScoped<CalendarFiles>();
        services.AddSingleton(provider => new ICalSchedule(provider.GetRequiredService<IServiceScopeFactory>(), Microsoft.Extensions.Logging.Abstractions.NullLogger<ICalSchedule>.Instance));
        services.AddSyncData();
        services.AddEventsData();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<EventsDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    protected async Task<T> WithAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    private Task WithAsync(Func<IServiceProvider, Task> work) => WithAsync(async provider =>
    {
        await work(provider);
        return true;
    });

    private protected Task<T> EditingAsync<T>(ClaimsPrincipal person, Func<EventEditingService, CallContext, ValueTask<T>> call) =>
        WithAsync(async provider => await call(provider.GetRequiredService<EventEditingService>(), As(person)));

    private protected Task<T> QueuedAsync<T>(ClaimsPrincipal person, string requestId, Func<EventEditingService, CallContext, ValueTask<T>> call) =>
        WithAsync(async provider => await call(provider.GetRequiredService<EventEditingService>(), As(person, requestId)));

    protected Task<CalendarSettings> CalendarAsync(string name, Audience audience, bool needsPublisher = false) =>
        WithAsync(async provider => await provider.GetRequiredService<CalendarSetupService>().SaveCalendarAsync(new CalendarSettings
        {
            Calendar = new CalendarInfo { Name = name, Audience = audience.Text },
            NeedsPublisher = needsPublisher,
        }, As(Person())));

    protected Task PublishAsync() => WithAsync(provider => provider.GetRequiredService<EventsPublisher>().PublishAsync(Cancel));

    protected async Task<List<FeedChangeRecord>> LiveRowsAsync(string feed)
    {
        List<FeedChangeRecord> rows = await WithAsync(provider => provider.GetRequiredService<EventsDbContext>().FeedChanges.AsNoTracking()
            .Where(change => change.Feed == feed).ToListAsync(Cancel));
        return rows.GroupBy(change => change.RecordId)
            .Select(group => group.MaxBy(change => change.Cursor)!)
            .Where(change => !change.Deleted)
            .ToList();
    }

    protected async Task<List<EventOccurrence>> OccurrencesAsync(string feed) =>
        (await LiveRowsAsync(feed)).Select(row => EventsPayloads.Decode<EventOccurrence>(row.Payload)).OrderBy(occurrence => occurrence.StartsAt).ToList();

    protected ClaimsPrincipal Person(params (string Node, string? CalendarId)[] grants)
    {
        Guid id = Guid.CreateVersion7();
        foreach ((string node, string? calendarId) in grants)
            Access.Allow(id, node, calendarId);
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim(Caller.UserIdClaim, id.ToString())], "test"));
    }

    protected static RegularPattern Weekly(DayOfWeek day, DateOnly? from = null) =>
        new() { Repeats = Repeats.Weekly, Days = [day], StartDate = EventDates.Format(from ?? Today.AddDays(-60)) };

    protected static CallContext As(ClaimsPrincipal person, string? requestId = null) => new(person, new PersonCallContext(person, requestId, Cancel));

    private sealed class PersonCallContext(ClaimsPrincipal person, string? requestId, CancellationToken cancellationToken) : ServerCallContext
    {
        private readonly Dictionary<object, object> _state = new() { ["__HttpContext"] = new DefaultHttpContext { User = person } };

        protected override IDictionary<object, object> UserStateCore => _state;
        protected override string MethodCore => "";
        protected override string HostCore => "";
        protected override string PeerCore => "";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => requestId is null ? [] : [new Metadata.Entry(QueuedRequest.IdHeader, requestId)];
        protected override CancellationToken CancellationTokenCore => cancellationToken;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(null, []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
