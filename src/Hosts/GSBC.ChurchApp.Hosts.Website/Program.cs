using GSBC.ChurchApp.Hosts.Website;
using GSBC.ChurchApp.Hosts.Website.Feeds;
using GSBC.ChurchApp.Platform.Client.Media;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProtoBuf.Grpc.Client;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenAnyIP(builder.Configuration.GetValue("Ports:Web", 8080), listen => listen.Protocols = HttpProtocols.Http1AndHttp2);
    kestrel.ListenAnyIP(builder.Configuration.GetValue("Ports:Grpc", 8081), listen => listen.Protocols = HttpProtocols.Http2);
});

WebsiteOptions options = builder.Configuration.GetSection(WebsiteOptions.Section).Get<WebsiteOptions>() ?? new WebsiteOptions();
builder.Services.AddSingleton(options);
builder.Services.AddSingleton<WebsiteLinks>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IMediaFiles, PublicMediaFiles>();
builder.Services.AddSingleton<PublicFeeds>();
builder.Services.AddSingleton(_ => GrpcChannel.ForAddress(builder.Configuration["Api:Address"] ?? "http://api:8081").CreateGrpcService<ISyncService>());
builder.Services.AddHostedService<FeedWatcher>();
builder.Services.AddRazorComponents();
builder.Services.AddGrpc(grpc => grpc.IgnoreUnknownServices = true);
builder.Services.AddGrpcHealthChecks(health =>
    {
        health.Services.Map("", result => result.Tags.Contains("live"));
        health.Services.Map("ready", result => result.Tags.Contains("ready"));
    })
    .AddCheck("live", () => HealthCheckResult.Healthy(), tags: ["live"])
    .Add(new HealthCheckRegistration("ready", services => services.GetRequiredService<PublicFeeds>(), null, ["ready"]));

WebApplication app = builder.Build();

app.UseWebsiteHeaders(app.Environment.IsDevelopment(), options);
app.MapStaticAssets();
app.MapGrpcHealthChecksService();
app.MapEmbedPages();

app.Run();
