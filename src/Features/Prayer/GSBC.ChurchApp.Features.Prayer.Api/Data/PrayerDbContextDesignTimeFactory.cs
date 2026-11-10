using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Prayer.Api.Data;

public sealed class PrayerDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PrayerDbContext>
{
    public PrayerDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PrayerDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, PrayerDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
