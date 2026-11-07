using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Grpc;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Events.Api.Tests;

public abstract class EventsHost : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private ServiceProvider _services = null!;

    internal ManualClock Clock { get; } = new(ChurchTime.ToUtc(new DateTime(2026, 11, 8, 20, 0, 0)));
    internal Grants Access { get; } = new();
    internal RecordedNotifications Notifications { get; } = new();

    protected static CancellationToken Cancel => TestContext.Current.CancellationToken;

    protected static DateOnly Today => new(2026, 11, 8);

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())])
            .Build());
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<IAccessEvaluator>(Access);
        services.AddSingleton<INotifications>(Notifications);
        services.AddSingleton<IMediaReferences, AnyMedia>();
        services.AddScoped<EventsPublisher>();
        services.AddScoped<EventWriter>();
        services.AddScoped<EventEditingService>();
        services.AddScoped<CalendarSetupService>();
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
        await _postgres.DisposeAsync();
    }

    protected async Task<T> WithAsync<T>(Func<IServiceProvider, Task<T>> work)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    protected Task WithAsync(Func<IServiceProvider, Task> work) => WithAsync<bool>(async provider =>
    {
        await work(provider);
        return true;
    });

    private protected Task<T> EditingAsync<T>(ClaimsPrincipal person, Func<EventEditingService, CallContext, ValueTask<T>> call) =>
        WithAsync(async provider => await call(provider.GetRequiredService<EventEditingService>(), As(person)));

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

    protected static CallContext As(ClaimsPrincipal person) => new(person, new PersonCallContext(person, Cancel));

    private sealed class PersonCallContext(ClaimsPrincipal person, CancellationToken cancellationToken) : ServerCallContext
    {
        private readonly Dictionary<object, object> _state = new() { ["__HttpContext"] = new DefaultHttpContext { User = person } };

        protected override IDictionary<object, object> UserStateCore => _state;
        protected override string MethodCore => "";
        protected override string HostCore => "";
        protected override string PeerCore => "";
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => cancellationToken;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new(null, []);

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}

internal sealed class ManualClock(DateTime utcNow) : TimeProvider
{
    public DateTime UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => new(UtcNow, TimeSpan.Zero);
}

internal sealed class Grants : IAccessEvaluator
{
    private readonly HashSet<(Guid, string, string?)> _allowed = [];

    public void Allow(Guid person, string node, string? calendarId) => _allowed.Add((person, node, calendarId));

    public ValueTask<bool> IsAllowedAsync(ClaimsPrincipal user, string node, string? scopeId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(Caller.UserId(user) is { } id && (_allowed.Contains((id, node, null)) || (scopeId is not null && _allowed.Contains((id, node, scopeId)))));

    public ValueTask<GrantSet> GrantsForAsync(Guid userId, CancellationToken cancellationToken) => ValueTask.FromResult(GrantSet.Empty);
}

internal sealed class RecordedNotifications : INotifications
{
    public List<Notification> Raised { get; } = [];

    public Task RaiseAsync(Notification notification, CancellationToken cancellationToken)
    {
        Raised.Add(notification);
        return Task.CompletedTask;
    }

    public Task SettleAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class AnyMedia : IMediaReferences
{
    public Task<IReadOnlyList<MediaRef>> VerifyAsync(IReadOnlyCollection<MediaRef> claimed, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MediaRef>>(claimed.ToList());

    public Task UseAsync(string owner, IReadOnlyCollection<MediaRef> verified, CancellationToken cancellationToken) => Task.CompletedTask;
}
