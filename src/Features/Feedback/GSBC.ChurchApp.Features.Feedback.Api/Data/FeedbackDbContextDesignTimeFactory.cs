using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GSBC.ChurchApp.Features.Feedback.Api.Data;

public sealed class FeedbackDbContextDesignTimeFactory : IDesignTimeDbContextFactory<FeedbackDbContext>
{
    public FeedbackDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<FeedbackDbContext>()
            .UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, FeedbackDbContext.SchemaName))
            .UseSnakeCaseNamingConvention()
            .Options);
}
