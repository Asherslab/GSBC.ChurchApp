using GSBC.ChurchApp.Features.Accounts.Gateway;
using GSBC.ChurchApp.Features.Events.Gateway;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenAnyIP(builder.Configuration.GetValue("Ports:Web", 8080), listen => listen.Protocols = HttpProtocols.Http1AndHttp2);
    kestrel.ListenAnyIP(builder.Configuration.GetValue("Ports:Grpc", 8081), listen => listen.Protocols = HttpProtocols.Http2);
});

builder.Services.AddAccountsGateway(builder.Configuration);
builder.Services.AddEventsGateway(builder.Configuration);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy")).AddApiTokens();
builder.Services.AddGrpc(options => options.IgnoreUnknownServices = true);
builder.Services.AddGrpcHealthChecks().AddCheck("live", () => HealthCheckResult.Healthy());

WebApplication app = builder.Build();

bool development = app.Environment.IsDevelopment();
app.Use((context, next) =>
{
    context.Response.OnStarting(() =>
    {
        if (development)
            context.Response.Headers.CacheControl = "no-store";
        else if (string.IsNullOrEmpty(context.Response.Headers.CacheControl))
            context.Response.Headers.CacheControl = "no-cache";
        return Task.CompletedTask;
    });
    return next(context);
});

app.UseAppAssociation();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.MapStaticAssets();
app.MapGrpcHealthChecksService();
app.MapPasskeyCeremonies();
app.MapCalendarFiles();
app.MapReverseProxy();
app.MapFallbackToFile("index.html");

app.Run();
