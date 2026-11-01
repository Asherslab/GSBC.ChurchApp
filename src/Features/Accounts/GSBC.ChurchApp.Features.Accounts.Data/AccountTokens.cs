namespace GSBC.ChurchApp.Features.Accounts.Data;

public static class AccountTokens
{
    public const string Issuer = "gsbc-gateway";
    public const string Audience = "gsbc-api";
    public const string SigningKeySetting = "Accounts:Tokens:SigningKey";
    public const string PublicKeySetting = "Accounts:Tokens:PublicKey";

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
}
