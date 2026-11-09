using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.People.Api.Data;

public sealed class PeopleDbContextDesignTimeFactory : IDesignTimeDbContextFactory<PeopleDbContext>
{
    public PeopleDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PeopleDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, PeopleDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
