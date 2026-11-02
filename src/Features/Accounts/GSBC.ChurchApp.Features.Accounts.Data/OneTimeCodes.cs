using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class OneTimeCodes(AccountsDbContext db, TimeProvider time)
{
    public static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan ConfirmationLifetime = TimeSpan.FromDays(1);
    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan RecoveryLifetime = TimeSpan.FromHours(2);
    public static readonly TimeSpan PhoneLinkLifetime = TimeSpan.FromMinutes(10);

    private const int TokenBytes = 32;
    private const int ShortCodeLength = 6;
    private const string ShortCodeAlphabet = "ACDEFGHJKMNPQRTWXY34679";
    private const int MaximumFailedAttempts = 5;

    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public string IssueToken(Guid userId, CodePurpose purpose, TimeSpan lifetime)
    {
        string token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenBytes));
        Add(Hash(token), userId, purpose, lifetime);
        return token;
    }

    public async Task<string> IssueDigitsAsync(Guid userId, CodePurpose purpose, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        await db.Codes
            .Where(code => code.UserId == userId && code.Purpose == purpose && code.UsedAt == null)
            .ExecuteDeleteAsync(cancellationToken);

        string digits = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        Add(Hash($"{userId}:{digits}"), userId, purpose, lifetime);
        return digits;
    }

    public string IssueShortCode(Guid userId, CodePurpose purpose, TimeSpan lifetime)
    {
        string code = new(Enumerable.Range(0, ShortCodeLength).Select(_ => ShortCodeAlphabet[RandomNumberGenerator.GetInt32(ShortCodeAlphabet.Length)]).ToArray());
        Add(Hash(code), userId, purpose, lifetime);
        return code;
    }

    public static string NormaliseShortCode(string typed) => new(typed.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    public Task<OneTimeCode?> FindTokenAsync(string token, IReadOnlyCollection<CodePurpose> purposes, CancellationToken cancellationToken)
    {
        string hash = Hash(token);
        DateTime now = Now;
        return db.Codes.SingleOrDefaultAsync(
            code => code.Hash == hash && purposes.Contains(code.Purpose) && code.UsedAt == null && code.ExpiresAt > now,
            cancellationToken);
    }

    public async Task<bool> ConsumeDigitsAsync(Guid userId, CodePurpose purpose, string digits, CancellationToken cancellationToken)
    {
        DateTime now = Now;
        OneTimeCode? code = await db.Codes.SingleOrDefaultAsync(
            candidate => candidate.UserId == userId && candidate.Purpose == purpose && candidate.UsedAt == null && candidate.ExpiresAt > now,
            cancellationToken);
        if (code is null || code.FailedAttempts >= MaximumFailedAttempts)
            return false;

        if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(code.Hash), Encoding.ASCII.GetBytes(Hash($"{userId}:{digits.Trim()}"))))
        {
            code.FailedAttempts++;
            return false;
        }

        Use(code);
        return true;
    }

    public void Use(OneTimeCode code) => code.UsedAt = Now;

    private void Add(string hash, Guid userId, CodePurpose purpose, TimeSpan lifetime) =>
        db.Codes.Add(new OneTimeCode
        {
            Hash = hash,
            UserId = userId,
            Purpose = purpose,
            CreatedAt = Now,
            ExpiresAt = Now + lifetime,
        });

    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
