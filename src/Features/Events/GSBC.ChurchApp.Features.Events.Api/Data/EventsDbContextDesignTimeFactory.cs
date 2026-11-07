using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Events.Api.Data;

public sealed class EventsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<EventsDbContext>
{
    public EventsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, EventsDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
