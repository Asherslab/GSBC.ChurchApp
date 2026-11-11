using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Rosters.Api.Data;

public sealed class RostersDbContextDesignTimeFactory : IDesignTimeDbContextFactory<RostersDbContext>
{
    public RostersDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<RostersDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, RostersDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
