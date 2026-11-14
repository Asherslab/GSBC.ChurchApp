using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Api.Data;

public sealed class OutreachMapsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OutreachMapsDbContext>
{
    public OutreachMapsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OutreachMapsDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, OutreachMapsDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
