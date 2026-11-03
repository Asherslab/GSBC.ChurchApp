using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Analytics.Api.Tests;

internal sealed class AnalyticsHost : IAsyncDisposable
{
    public static readonly Guid Owner = Guid.NewGuid();

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private ServiceProvider _services = null!;

    public MovableClock Clock { get; } = new(new DateTimeOffset(2026, 11, 1, 23, 0, 0, TimeSpan.Zero));

    public RecordingEmail Email { get; } = new();

    public DateOnly Today => ChurchTime.Today(Clock);

    public async Task StartAsync(params ISubjectNames[] subjectNames)
    {
        await _postgres.StartAsync();
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Database.ConnectionStringName}"] = _postgres.GetConnectionString(),
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
            .AddSingleton(new MeasureCatalog([new MeasureSet([.. AnalyticsMeasures.All, Measures.TileTapped])]))
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
        await _postgres.DisposeAsync();
    }

    public static class Measures
    {
        public static readonly Platform.Contracts.Analytics.Measure TileTapped =
            new("hub.tile.tapped", "Home screen tiles tapped", "Taps on each tile", Platform.Contracts.Analytics.SubjectKinds.HubTile, SplitByGroup: true);
    }

    internal sealed class MovableClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
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
        public ValueTask<IReadOnlyList<Guid>> HoldersOfAsync(string node, CancellationToken cancellationToken) => ValueTask.FromResult(holders);
    }

    private sealed class Emails(Dictionary<Guid, string> emails) : IAccountEmails
    {
        public ValueTask<IReadOnlyList<string>> EmailsOfAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            ValueTask.FromResult<IReadOnlyList<string>>(userIds.Where(emails.ContainsKey).Select(id => emails[id]).ToList());
    }
}
