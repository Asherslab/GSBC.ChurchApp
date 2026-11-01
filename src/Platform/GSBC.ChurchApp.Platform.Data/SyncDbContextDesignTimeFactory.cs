using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Platform.Data;

public sealed class SyncDbContextDesignTimeFactory : IDesignTimeDbContextFactory<SyncDbContext>
{
    public SyncDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SyncDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, FeedChangeTable.Schema))
            .UseSnakeCaseNamingConvention()
            .Options);
}
