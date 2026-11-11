using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Chats.Api.Data;

public sealed class ChatsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ChatsDbContext>
{
    public ChatsDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<ChatsDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, ChatsDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
