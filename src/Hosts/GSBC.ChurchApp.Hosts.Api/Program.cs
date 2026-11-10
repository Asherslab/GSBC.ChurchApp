using GSBC.ChurchApp.Features.Access.Api;
using GSBC.ChurchApp.Features.Analytics.Api;
using GSBC.ChurchApp.Features.Chats.Api;
using GSBC.ChurchApp.Features.Accounts.Api;
using GSBC.ChurchApp.Features.Configuration.Api;
using GSBC.ChurchApp.Features.Events.Api;
using GSBC.ChurchApp.Features.Media.Api;
using GSBC.ChurchApp.Features.Notifications.Api;
using GSBC.ChurchApp.Features.Ministries.Api;
using GSBC.ChurchApp.Features.People.Api;
using GSBC.ChurchApp.Features.Sermons.Api;
using GSBC.ChurchApp.Features.Sermons.Import.Api;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Contracts.Sync;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddPlatformApi();
builder.Services.AddAccountTokenValidation(builder.Configuration);
builder.Services.AddAccountsApi(builder.Configuration);
builder.Services.AddAccessApi();
builder.Services.AddPeopleApi(builder.Configuration);
builder.Services.AddMinistriesApi();
builder.Services.AddChatsApi();
builder.Services.AddConfigurationApi();
builder.Services.AddMediaApi(builder.Configuration);
builder.Services.AddAnalyticsApi();
builder.Services.AddNotificationsApi(builder.Configuration);
builder.Services.AddSermonsApi(builder.Configuration);
builder.Services.AddSermonsImportApi(builder.Configuration);
builder.Services.AddEventsApi();

if (builder.Environment.IsDevelopment())
    builder.Services.AddFeed(new FeedDefinition(DevelopmentFeeds.Echo, FeedAudience.Public));

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseRateLimiter();
app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
app.MapPlatformApi();
app.MapAccountsApi();
app.MapAccessApi();
app.MapPeopleApi();
app.MapMinistriesApi();
app.MapChatsApi();
app.MapConfigurationApi();
app.MapMediaApi();
app.MapAnalyticsApi();
app.MapNotificationsApi();
app.MapSermonsApi();
app.MapSermonsImportApi();
app.MapEventsApi();

app.Run();
