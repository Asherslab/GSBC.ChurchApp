using GSBC.ChurchApp.Features.Accounts.Api;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using Grpc.AspNetCore.Web;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddPlatformApi();
builder.Services.AddAccountTokenValidation(builder.Configuration);
builder.Services.AddAccountsApi(builder.Configuration);

if (builder.Environment.IsDevelopment())
    builder.Services.AddFeed(new FeedDefinition(DevelopmentFeeds.Echo, FeedAudience.Public));

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseRateLimiter();
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.MapPlatformApi();
app.MapAccountsApi();

app.Run();
