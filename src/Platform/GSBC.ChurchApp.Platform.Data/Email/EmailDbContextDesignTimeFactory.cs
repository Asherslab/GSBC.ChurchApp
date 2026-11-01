using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Platform.Data.Email;

public sealed class EmailDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EmailDbContext>
{
    public EmailDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<EmailDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, EmailDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
