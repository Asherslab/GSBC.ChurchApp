using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Media.Api.Data;

public sealed class MediaDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MediaDbContext>
{
    public MediaDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, MediaDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
