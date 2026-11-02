using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.RateLimiting;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Api.Tiers;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Grpc.Server;

namespace GSBC.ChurchApp.Platform.Api.Hosting;

public static class PlatformApiExtensions
{
    public const string ReadinessService = "ready";
    public const int DefaultWebPort = 8080;
    public const int DefaultGrpcPort = 8081;

    public static WebApplicationBuilder AddPlatformApi(this WebApplicationBuilder builder)
    {
        IServiceCollection services = builder.Services;

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.ListenAnyIP(builder.Configuration.GetValue("Ports:Web", DefaultWebPort), listen => listen.Protocols = HttpProtocols.Http1AndHttp2);
            kestrel.ListenAnyIP(builder.Configuration.GetValue("Ports:Grpc", DefaultGrpcPort), listen => listen.Protocols = HttpProtocols.Http2);
        });

        services.AddCodeFirstGrpc(options =>
        {
            options.Interceptors.Add<TierInterceptor>();
            options.IgnoreUnknownServices = true;
        });
        services.AddSingleton<TierCatalog>();
        services.TryAddScoped<IAccessEvaluator, NoGrantsAccessEvaluator>();
        services.AddSingleton<PermissionCatalog>();
        services.AddSingleton<FeedRegistry>();
        services.AddSingleton<FeedChangeBroadcaster>();
        services.AddSingleton(provider =>
        {
            FeedChangeBroadcaster broadcaster = provider.GetRequiredService<FeedChangeBroadcaster>();
            return new NotificationHandler(
                FeedChangeTable.NotifyChannel,
                payload => broadcaster.PublishChange(payload[..payload.LastIndexOf(':')]),
                broadcaster.PublishAllMayHaveChanged);
        });
        services.AddHostedService<PostgresNotificationListener>();
        services.AddScoped<FeedRetention>();
        services.AddHostedService<FeedRetentionService>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddPlatformRateLimits();
        services.AddEmailData();
        services.AddSingleton<EmailSignal>();
        services.AddScoped<IEmailSender, QueuedEmailSender>();
        services.AddScoped<EmailDispatcher>();
        services.AddHostedService<EmailDelivery>();
        if (builder.Configuration.HasGraphEmail())
            services.AddGraphEmail(builder.Configuration);
        else if (builder.Environment.IsDevelopment())
            services.TryAddSingleton<IEmailTransport, DevelopmentEmailTransport>();
        else
            services.TryAddSingleton<IEmailTransport, UnconfiguredEmailTransport>();
        services.AddSyncData();

        services
            .AddGrpcHealthChecks(options =>
            {
                options.Services.Map("", _ => false);
                options.Services.Map(ReadinessService, result => result.Tags.Contains(ReadinessService));
            })
            .AddDbContextCheck<SyncDbContext>(tags: [ReadinessService]);

        return builder;
    }

    public static WebApplication MapPlatformApi(this WebApplication app)
    {
        app.MapGrpcHealthChecksService();
        app.MapGrpcService<SyncService>();
        return app;
    }

    public static IServiceCollection AddPermissions(this IServiceCollection services, params IReadOnlyList<Permission> permissions) =>
        services.AddSingleton(new PermissionSet(permissions));

    public static IServiceCollection AddFeed(this IServiceCollection services, FeedDefinition feed) =>
        services.AddSingleton(feed);

    public static IServiceCollection AddSystemRole(this IServiceCollection services, SystemRole role) =>
        services.AddSingleton(role);
}
