using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Notifications.Api.Data;

public sealed class NotificationsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<NotificationsDbContext>
{
    public NotificationsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, NotificationsDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
