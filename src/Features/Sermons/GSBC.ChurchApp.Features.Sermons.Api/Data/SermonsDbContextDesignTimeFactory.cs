using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Sermons.Api.Data;

public sealed class SermonsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SermonsDbContext>
{
    public SermonsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SermonsDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, SermonsDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
