using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.News.Api.Data;

public sealed class NewsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<NewsDbContext>
{
    public NewsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<NewsDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, NewsDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
