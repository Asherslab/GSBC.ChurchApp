using GSBC.ChurchApp.Platform.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Accounts.Data;

public sealed class AccountsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AccountsDbContext>
{
    public AccountsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AccountsDbContext>()
            .UseApplicationServiceProvider(new ServiceCollection()
                .Configure<IdentityOptions>(AccountsDataExtensions.UsePasskeySchema)
                .BuildServiceProvider())
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, AccountsDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .Options);
}
