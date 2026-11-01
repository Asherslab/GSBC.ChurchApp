using System.Security.Claims;
using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GSBC.ChurchApp.Features.Accounts.Gateway;

public sealed class ApiTokenIssuer(IConfiguration configuration, TimeProvider time)
{
    private readonly Lazy<SigningCredentials?> _credentials = new(() => Credentials(configuration[AccountTokens.SigningKeySetting]));
    private readonly JsonWebTokenHandler _handler = new();

    public string? Issue(ClaimsPrincipal user)
    {
        if (_credentials.Value is not { } credentials || user.FindFirstValue(ClaimTypes.NameIdentifier) is not { } userId)
            return null;

        DateTime now = time.GetUtcNow().UtcDateTime;
        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = AccountTokens.Issuer,
            Audience = AccountTokens.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now + AccountTokens.Lifetime,
            SigningCredentials = credentials,
            Claims = new Dictionary<string, object>
            {
                [AccountClaims.UserId] = userId,
                [AccountClaims.SessionId] = user.FindFirstValue(AccountClaims.SessionId) ?? "",
                [AccountClaims.Name] = user.FindFirstValue(AccountClaims.Name) ?? "",
            },
        });
    }

    private static SigningCredentials? Credentials(string? pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
            return null;

        ECDsa key = ECDsa.Create();
        key.ImportFromPem(pem);
        return new SigningCredentials(new ECDsaSecurityKey(key), SecurityAlgorithms.EcdsaSha256);
    }
}
