using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

internal sealed class AnalyticsHost : IAsyncDisposable
{
    private static readonly Guid Owner = Guid.NewGuid();

    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    public TestClock Clock { get; } = new(new DateTime(2026, 11, 1, 23, 0, 0, DateTimeKind.Utc));

    public RecordingEmail Email { get; } = new();

    public DateOnly Today => ChurchTime.Today(Clock);

    public async Task StartAsync(params ISubjectNames[] subjectNames)
    {
        _database = await TestDatabase.CreateAsync();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Database.ConnectionStringName}"] = _database.ConnectionString,
            ["Accounts:SiteUrl"] = "https://uat-app.baptist.com.au",
        }).Build();

        ServiceCollection services = new();
        services
            .AddSingleton(configuration)
            .AddLogging()
            .AddSingleton<TimeProvider>(Clock)
            .AddSingleton<IPermissionHolders>(new Holders([Owner]))
            .AddSingleton<IAccountEmails>(new Emails(new Dictionary<Guid, string> { [Owner] = "owner@example.org" }))
            .AddSingleton<IEmailSender>(Email)
            .AddSingleton(TestMeasures.Catalog)
            .AddAnalyticsData()
            .AddSingleton<AnalyticsEnvironment>()
            .AddScoped<NewErrorKinds>()
            .AddScoped<ReportsSeen>()
            .AddScoped<DiagnosticsInbox>()
            .AddScoped<UsageInbox>()
            .AddScoped<AnalyticsRetention>()
            .AddScoped<HealthService>()
            .AddScoped<InsightsService>();
        foreach (ISubjectNames naming in subjectNames)
            services.AddSingleton(naming);
        _services = services.BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>().Database.MigrateAsync(Cancellation);
    }

    public async Task<TResult> WithAsync<TService, TResult>(Func<TService, Task<TResult>> use) where TService : notnull
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        return await use(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task WithAsync<TService>(Func<TService, Task> use) where TService : notnull
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await use(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public static CancellationToken Cancellation => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }

    internal sealed class RecordingEmail : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class Holders(IReadOnlyList<Guid> holders) : IPermissionHolders
    {
        public ValueTask<PermissionHolders> HoldersAsync(Permission permission, string? objectId, CancellationToken cancellationToken) =>
            ValueTask.FromResult(new PermissionHolders(holders, []));
    }

    private sealed class Emails(Dictionary<Guid, string> emails) : IAccountEmails
    {
        public ValueTask<IReadOnlyList<string>> EmailsOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<string>>(userIds.Where(emails.ContainsKey).Select(id => emails[id]).ToList());
    }
}
