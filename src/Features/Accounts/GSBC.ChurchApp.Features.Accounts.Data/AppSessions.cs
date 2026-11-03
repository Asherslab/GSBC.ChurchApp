using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public static class AppSessions
{
    public const string Header = "X-Gsbc-Session";
    public const string EndedHeader = "X-Gsbc-Session-Ended";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(90);

    private const int SecretBytes = 32;
    private const int MaximumDeviceNameLength = 200;

    public static string Start(AccountsDbContext db, Guid userId, string? deviceName, DateTime now, Guid? reviewCodeId = null)
    {
        string secret = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(SecretBytes));
        string device = deviceName?.Trim() ?? "";
        db.Sessions.Add(new AccountSession
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Client = SessionClient.App,
            DeviceName = device.Length == 0 ? "A phone" : device[..Math.Min(device.Length, MaximumDeviceNameLength)],
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now + Lifetime,
            SecretHash = HashOf(secret),
            ReviewCodeId = reviewCodeId,
        });
        return secret;
    }

    public static string HashOf(string secret) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
