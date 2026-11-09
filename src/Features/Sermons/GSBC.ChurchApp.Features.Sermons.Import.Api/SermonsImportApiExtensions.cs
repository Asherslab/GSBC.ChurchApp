using GSBC.ChurchApp.Features.Sermons.Import.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Import.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Sync;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Sermons.Import.Api;

public static class SermonsImportApiExtensions
{
    public static IServiceCollection AddSermonsImportApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSermonsImportData();
        services.Configure<ImportOptions>(configuration.GetSection(ImportOptions.Section));
        services.AddSingleton<PageGate>();
        services.AddHttpClient<ArchiveClient>(client => client.Timeout = TimeSpan.FromMinutes(10));
        services.AddScoped<SermonImporter>();
        services.AddSingleton<ImportRunner>();
        services.AddHostedService(provider => provider.GetRequiredService<ImportRunner>());
        services.AddPermissions(SermonsImportPermissions.All);
        services.AddFeed(new FeedDefinition(SermonsImportFeeds.Progress, FeedAudience.Holding(SermonsImportPermissions.Import)));
        return services;
    }

    public static IEndpointRouteBuilder MapSermonsImportApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<SermonImportService>();
        return endpoints;
    }
}
