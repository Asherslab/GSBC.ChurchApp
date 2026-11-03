using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Analytics.Api.Data;

public sealed class AnalyticsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AnalyticsDbContext>
{
    public AnalyticsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AnalyticsDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, AnalyticsDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
