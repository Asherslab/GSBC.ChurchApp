using System.Security.Claims;
using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace GSBC.ChurchApp.Features.Feedback.Api.Tests;

public abstract class FeedbackHost : IAsyncLifetime
{
    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    internal TestClock Clock { get; } = new(ChurchTime.ToUtc(new DateTime(2026, 11, 11, 9, 0, 0)));
    internal RecordedNotifications Told { get; } = new();
    internal Grants Access { get; } = new();
    internal Library Media { get; } = new();
    internal KnownPeople People { get; } = new();

    protected static CancellationToken Cancel => TestContext.Current.CancellationToken;

    protected static readonly Guid Staff = Guid.CreateVersion7();

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        ServiceCollection services = new();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
            .Build());
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<INotifications>(Told);
        services.AddSingleton<IAccessEvaluator>(Access);
        services.AddSingleton<IMediaReferences>(Media);
        services.AddSingleton<IMediaSystemFolders>(Media);
        services.AddSingleton<IPeopleIndex>(People);
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = "Testing" });
        services.AddScoped<FeedbackSenders>().AddScoped<FeedbackSending>().AddScoped<FeedbackDesk>().AddScoped<FeedbackMedia>()
            .AddScoped<FeedbackTelling>().AddScoped<FeedbackKeeping>().AddScoped<HandledRequests>().AddScoped<FeedbackPersonData>();
        services.AddSyncData();
        services.AddFeedbackData();
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<FeedbackDbContext>().Database.MigrateAsync();
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

    protected Task WithAsync(Func<IServiceProvider, Task> work) => WithAsync(async provider =>
    {
        await work(provider);
        return true;
    });

    private static ClaimsPrincipal SignedIn(Guid person) => new(new ClaimsIdentity([new Claim(Caller.UserIdClaim, person.ToString())], "test"));

    protected async Task<string> SendAsync(Guid? install, Guid? person, string text = "The Save button is cut off", IReadOnlyList<string>? attachments = null,
        string phoneData = "", string? id = null, FeedbackSource source = FeedbackSource.App)
    {
        FeedbackSend send = new()
        {
            Id = id ?? Guid.NewGuid().ToString(), Text = text, AttachmentIds = [.. attachments ?? []], PhoneDataId = phoneData, Source = source,
            Phone = new PhoneDetails { AppVersion = "1.4", AppBuild = "212", Platform = "ios", OsVersion = "18.1", Model = "iPhone15,4" },
            Browser = new BrowserDetails { Browser = "Safari 18", System = "macOS 15", Window = "1440 × 900", ColourScheme = "light", PageTitle = "Media", RecentPages = ["/media", "/"] },
        };
        await WithAsync(async provider =>
        {
            ClaimsPrincipal user = person is { } signedIn ? SignedIn(signedIn) : new ClaimsPrincipal();
            Sender sender = await provider.GetRequiredService<FeedbackSenders>().WhoAsync(user, install, Cancel);
            await provider.GetRequiredService<FeedbackSending>().SendAsync(sender, send, Cancel);
        });
        return send.Id;
    }

    private protected Task DeskAsync(Func<FeedbackDesk, Task> work) => WithAsync(provider => work(provider.GetRequiredService<FeedbackDesk>()));

    private protected Task<TicketRecord?> RecordAsync(string id) =>
        WithAsync(provider => provider.GetRequiredService<FeedbackDbContext>().Tickets.AsNoTracking().Include(ticket => ticket.Attachments)
            .FirstOrDefaultAsync(ticket => ticket.Id == Guid.Parse(id), Cancel));
}
