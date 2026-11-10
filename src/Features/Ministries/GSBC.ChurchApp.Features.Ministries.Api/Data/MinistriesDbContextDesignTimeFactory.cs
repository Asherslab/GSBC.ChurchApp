using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Ministries.Api.Data;

public sealed class MinistriesDbContextDesignTimeFactory : IDesignTimeDbContextFactory<MinistriesDbContext>
{
    public MinistriesDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<MinistriesDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, MinistriesDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
