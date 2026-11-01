using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Data;

public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddFeatureDbContext<TContext>(this IServiceCollection services, string schema, int migrationOrder)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((provider, options) =>
            options.UseNpgsql(
                provider.GetRequiredService<IConfiguration>().GetConnectionString(Database.ConnectionStringName),
                npgsql => npgsql.MigrationsHistoryTable(Database.MigrationsHistoryTable, schema))
            .UseSnakeCaseNamingConvention());

        services.AddSingleton(new MigrationTarget(typeof(TContext), migrationOrder));
        return services;
    }

    public static IServiceCollection AddEmailData(this IServiceCollection services) =>
        services.AddFeatureDbContext<Email.EmailDbContext>(Email.EmailDbContext.SchemaName, migrationOrder: 1);

    public static IServiceCollection AddSyncData(this IServiceCollection services) =>
        services.AddFeatureDbContext<SyncDbContext>(FeedChangeTable.Schema, migrationOrder: 0);
}
