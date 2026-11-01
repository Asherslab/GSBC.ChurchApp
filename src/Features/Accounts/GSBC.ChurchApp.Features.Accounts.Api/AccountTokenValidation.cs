using System.Security.Cryptography;
using GSBC.ChurchApp.Features.Accounts.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GSBC.ChurchApp.Features.Accounts.Api;

public static class AccountTokenValidation
{
    public static IServiceCollection AddAccountTokenValidation(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = AccountTokens.Issuer,
                    ValidAudience = AccountTokens.Audience,
                    ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
                    IssuerSigningKeys = PublicKeys(configuration[AccountTokens.PublicKeySetting]),
                    NameClaimType = AccountClaims.Name,
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });
        return services;
    }

    private static IEnumerable<SecurityKey> PublicKeys(string? pem)
    {
        if (string.IsNullOrWhiteSpace(pem))
            return [];

        ECDsa key = ECDsa.Create();
        key.ImportFromPem(pem);
        return [new ECDsaSecurityKey(key)];
    }
}
