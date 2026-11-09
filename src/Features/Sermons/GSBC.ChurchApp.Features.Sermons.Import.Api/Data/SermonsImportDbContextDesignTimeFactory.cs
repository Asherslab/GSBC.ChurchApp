using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api.Data;

public sealed class SermonsImportDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SermonsImportDbContext>
{
    public SermonsImportDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SermonsImportDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, SermonsImportDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
