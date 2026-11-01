using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Access.Api.Data;

public sealed class AccessDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AccessDbContext>
{
    public AccessDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AccessDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, AccessDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
