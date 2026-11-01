using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GSBC.ChurchApp.Platform.Api.Sync;

internal sealed class FeedChangeListener(
    IConfiguration configuration, FeedChangeBroadcaster broadcaster, ILogger<FeedChangeListener> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ListenAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Lost the feed change listener; reconnecting");
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
        }
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        await using NpgsqlConnection connection = new(configuration.GetConnectionString(Database.ConnectionStringName));
        connection.Notification += (_, notification) => broadcaster.PublishChange(FeedOf(notification.Payload));
        await connection.OpenAsync(stoppingToken);

        await using (NpgsqlCommand listen = new($"LISTEN {FeedChangeTable.NotifyChannel}", connection))
            await listen.ExecuteNonQueryAsync(stoppingToken);

        broadcaster.PublishAllMayHaveChanged();

        while (true)
            await connection.WaitAsync(stoppingToken);
    }

    private static string FeedOf(string payload) => payload[..payload.LastIndexOf(':')];
}
