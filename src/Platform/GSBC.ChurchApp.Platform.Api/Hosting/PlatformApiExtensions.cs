using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Analytics;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Api.Email;
using GSBC.ChurchApp.Platform.Api.Installs;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.RateLimiting;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Api.Tiers;
using GSBC.ChurchApp.Platform.Contracts.Analytics;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using ProtoBuf.Grpc.Server;

namespace GSBC.ChurchApp.Platform.Api.Hosting;

public static class PlatformApiExtensions
{
    public const string ReadinessService = "ready";
    private const string LivenessTag = "live";
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
        services.AddSingleton<InstallProofMethods>();
        services.TryAddScoped<IAccessEvaluator, NoGrantsAccessEvaluator>();
        services.TryAddScoped<ISignedInSessions, UncheckedSessions>();
        services.TryAddSingleton<IScopeHierarchy, FlatScopeHierarchy>();
        services.TryAddScoped<IInstallVerifier, NoInstallVerifier>();
        services.TryAddScoped<INotifications, NoNotifications>();
        services.TryAddScoped<IPeopleIndex, NoPeople>();
        services.TryAddScoped<IPermissionHolders, NoPermissionHolders>();
        services.TryAddScoped<IAccountEmails, NoAccountEmails>();
        services.AddSingleton<PermissionCatalog>();
        services.AddSingleton<MeasureCatalog>();
        services.AddSingleton<FeedRegistry>();
        services.AddSingleton<DestinationCatalog>();
        services.AddScoped<DestinationDirectory>();
        services.AddDestinations(ShellDestinations.All);
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
                options.Services.Map("", result => result.Tags.Contains(LivenessTag));
                options.Services.Map(ReadinessService, result => result.Tags.Contains(ReadinessService));
            })
            .AddCheck(LivenessTag, () => HealthCheckResult.Healthy(), tags: [LivenessTag])
            .AddDbContextCheck<SyncDbContext>(tags: [ReadinessService]);

        return builder;
    }

    public static WebApplication MapPlatformApi(this WebApplication app)
    {
        app.UseMiddleware<InstallProofMiddleware>();
        app.MapGrpcHealthChecksService();
        app.MapGrpcService<SyncService>();
        app.MapGrpcService<DestinationsService>();
        return app;
    }

    public static IServiceCollection AddPermissions(this IServiceCollection services, params IReadOnlyList<Permission> permissions) =>
        services.AddSingleton(new PermissionSet(permissions));

    public static IServiceCollection AddMeasures(this IServiceCollection services, params IReadOnlyList<Measure> measures) =>
        services.AddSingleton(new MeasureSet(measures));

    public static IServiceCollection AddFeed(this IServiceCollection services, FeedDefinition feed) =>
        services.AddSingleton(feed);

    public static IServiceCollection AddSurfaces(this IServiceCollection services, SurfaceSet surfaces)
    {
        services.TryAddSingleton<SurfaceCatalog>();
        return services.AddSingleton(surfaces);
    }

    public static IServiceCollection AddNotificationKinds(this IServiceCollection services, params IReadOnlyList<NotificationKind> kinds) =>
        services.AddSingleton(new NotificationKindSet(kinds));

    public static IServiceCollection AddDestinations(this IServiceCollection services, DestinationSet destinations) =>
        services.AddSingleton(destinations);

    public static IServiceCollection AddDestinationSource<TSource>(this IServiceCollection services) where TSource : class, IDestinationSource =>
        services.AddScoped<IDestinationSource, TSource>();

    public static IServiceCollection AddSystemRole(this IServiceCollection services, SystemRole role) =>
        services.AddSingleton(role);

    public static IServiceCollection AddToSystemRole(this IServiceCollection services, string roleKey, params IReadOnlyList<string> patterns) =>
        services.AddSingleton(new SystemRolePatterns(roleKey, patterns));
}
