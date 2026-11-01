using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Data.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class EmailDispatcherTests : IAsyncLifetime
{
    private static readonly DateTime Start = new(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private ServiceProvider _services = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _postgres.GetConnectionString())])
                .Build())
            .AddEmailData()
            .BuildServiceProvider();

        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<EmailDbContext>().Database.MigrateAsync();
    }

    [Fact]
    public async Task AFailedSendWaitsOutItsBackoffAndThenGoesOut()
    {
        await using (AsyncServiceScope scope = _services.CreateAsyncScope())
        {
            EmailDbContext db = scope.ServiceProvider.GetRequiredService<EmailDbContext>();
            db.OutgoingEmails.Add(new OutgoingEmail { To = "ada@example.com", Subject = "Hello", Body = "Hi", CreatedAt = Start, NextAttemptAt = Start });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        FlakyTransport transport = new(failures: 1);
        Assert.Equal(1, await DeliverAsync(transport, Start));
        Assert.Equal(0, await DeliverAsync(transport, Start.AddSeconds(30)));
        Assert.Equal(1, await DeliverAsync(transport, Start + EmailDispatcher.Backoff(1)));

        await using AsyncServiceScope check = _services.CreateAsyncScope();
        OutgoingEmail email = await check.ServiceProvider.GetRequiredService<EmailDbContext>().OutgoingEmails.SingleAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(email.SentAt);
        Assert.Equal(1, email.Attempts);
        Assert.Equal(["ada@example.com"], transport.Delivered);
    }

    private async Task<int> DeliverAsync(IEmailTransport transport, DateTime now)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        EmailDispatcher dispatcher = new(scope.ServiceProvider.GetRequiredService<EmailDbContext>(), transport, NullLogger<EmailDispatcher>.Instance);
        return await dispatcher.DeliverDueAsync(now, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class FlakyTransport(int failures) : IEmailTransport
    {
        private int _remainingFailures = failures;

        public List<string> Delivered { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (_remainingFailures-- > 0)
                throw new InvalidOperationException("Mail server unavailable");
            Delivered.Add(message.To);
            return Task.CompletedTask;
        }
    }
}
