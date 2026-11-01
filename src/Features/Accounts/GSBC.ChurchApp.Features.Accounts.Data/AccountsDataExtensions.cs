using GSBC.ChurchApp.Platform.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public static class AccountsDataExtensions
{
    public static IServiceCollection AddAccountsData(this IServiceCollection services) =>
        services
            .Configure<IdentityOptions>(UsePasskeySchema)
            .AddFeatureDbContext<AccountsDbContext>(AccountsDbContext.Schema, migrationOrder: 10)
            .AddScoped<OneTimeCodes>()
            .AddScoped<AccountsAudit>();

    public static void UsePasskeySchema(IdentityOptions options) =>
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
}
