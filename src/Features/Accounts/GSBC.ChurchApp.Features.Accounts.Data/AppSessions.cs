using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public static class AppSessions
{
    public const string Header = "X-Gsbc-Session";
    public const string EndedHeader = "X-Gsbc-Session-Ended";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);
    public static readonly TimeSpan ToolLifetime = TimeSpan.FromDays(30);
    public const int MaximumToolNameLength = 80;

    private const int SecretBytes = 32;
    private const int MaximumDeviceNameLength = 200;

    public static string Start(AccountsDbContext db, Guid userId, string? deviceName, DateTime now, Guid? reviewCodeId = null)
    {
        string device = deviceName?.Trim() ?? "";
        return Add(db, userId, SessionClient.App, device.Length == 0 ? "A phone" : device[..Math.Min(device.Length, MaximumDeviceNameLength)], now, reviewCodeId);
    }

    public static string StartTool(AccountsDbContext db, Guid userId, string name, DateTime now) =>
        Add(db, userId, SessionClient.Tool, name, now, reviewCodeId: null);

    public static TimeSpan LifetimeOf(SessionClient client) => client == SessionClient.Tool ? ToolLifetime : Lifetime;

    private static string Add(AccountsDbContext db, Guid userId, SessionClient client, string deviceName, DateTime now, Guid? reviewCodeId)
    {
        string secret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(SecretBytes));
        db.Sessions.Add(new AccountSession
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Client = client,
            DeviceName = deviceName,
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now + LifetimeOf(client),
            SecretHash = HashOf(secret),
            ReviewCodeId = reviewCodeId,
        });
        return secret;
    }

    public static string HashOf(string secret) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
