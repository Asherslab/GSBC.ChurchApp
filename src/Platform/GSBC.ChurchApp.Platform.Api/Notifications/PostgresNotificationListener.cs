using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace GSBC.ChurchApp.Platform.Api.Notifications;

public sealed record NotificationHandler(string Channel, Action<string> OnNotification, Action OnReconnected);

internal sealed class PostgresNotificationListener(
    IConfiguration configuration, IEnumerable<NotificationHandler> handlers, ILogger<PostgresNotificationListener> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private readonly ILookup<string, NotificationHandler> _byChannel = handlers.ToLookup(handler => handler.Channel);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_byChannel.Count == 0)
            return;

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
                logger.LogWarning(exception, "Lost the Postgres notification listener; reconnecting");
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
        }
    }

    private async Task ListenAsync(CancellationToken stoppingToken)
    {
        await using NpgsqlConnection connection = new(configuration.GetConnectionString(Database.ConnectionStringName));
        connection.Notification += (_, notification) =>
        {
            foreach (NotificationHandler handler in _byChannel[notification.Channel])
                handler.OnNotification(notification.Payload);
        };
        await connection.OpenAsync(stoppingToken);

        foreach (string channel in _byChannel.Select(group => group.Key))
        {
            await using NpgsqlCommand listen = new($"LISTEN {channel}", connection);
            await listen.ExecuteNonQueryAsync(stoppingToken);
        }

        foreach (NotificationHandler handler in _byChannel.SelectMany(group => group))
            handler.OnReconnected();

        while (!stoppingToken.IsCancellationRequested)
            await connection.WaitAsync(stoppingToken);
    }
}
