using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Outreach.Api.Data;

public sealed class OutreachDbContextDesignTimeFactory : IDesignTimeDbContextFactory<OutreachDbContext>
{
    public OutreachDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OutreachDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, OutreachDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
