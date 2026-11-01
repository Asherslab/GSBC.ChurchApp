using Microsoft.AspNetCore.Server.Kestrel.Core;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.ListenAnyIP(builder.Configuration.GetValue("Ports:Web", 8080), listen => listen.Protocols = HttpProtocols.Http1AndHttp2);
    kestrel.ListenAnyIP(builder.Configuration.GetValue("Ports:Grpc", 8081), listen => listen.Protocols = HttpProtocols.Http2);
});

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddGrpc(options => options.IgnoreUnknownServices = true);
builder.Services.AddGrpcHealthChecks();

WebApplication app = builder.Build();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.MapGrpcHealthChecksService();
app.MapReverseProxy();
app.MapFallbackToFile("index.html");

app.Run();
