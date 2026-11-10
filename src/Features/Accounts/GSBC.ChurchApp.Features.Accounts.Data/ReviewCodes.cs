using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Contracts;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public static class ReviewCodes
{
    public const string Alphabet = "ACDEFGHJKMNPQRTUVWXY34679";
    public const int LabelLength = 80;
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(30);
    public static readonly TimeSpan LongestLifetime = TimeSpan.FromDays(180);

    public static string NewCode()
    {
        char[] characters = RandomNumberGenerator.GetItems<char>(Alphabet, AccountsCodes.ReviewLength);
        return CodeGroups.Format(new string(characters), AccountsCodes.ReviewLength);
    }

    public static string Normalise(string typed) =>
        new(typed.ToUpperInvariant().Where(char.IsAsciiLetterOrDigit).ToArray());

    public static string HashOf(string typed) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Normalise(typed))));

    public static Expression<Func<AccountSession, bool>> SessionIsCurrent(AccountsDbContext db, DateTime now) =>
        session => session.ReviewCodeId == null
            || db.ReviewCodes.Any(code => code.Id == session.ReviewCodeId && code.TurnedOffAt == null && code.ExpiresAt > now);
}
