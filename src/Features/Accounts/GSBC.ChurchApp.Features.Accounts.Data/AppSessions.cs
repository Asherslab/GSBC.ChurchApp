using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public static class AppSessions
{
    public const string Header = "X-Gsbc-Session";
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    private const int SecretBytes = 32;

    public static string NewSecret() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(SecretBytes));

    public static string HashOf(string secret) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
}
