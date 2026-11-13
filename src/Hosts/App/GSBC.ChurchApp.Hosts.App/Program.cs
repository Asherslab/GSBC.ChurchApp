using GSBC.ChurchApp.Features.Access.Client;
using GSBC.ChurchApp.Features.Accounts.App;
using GSBC.ChurchApp.Features.Analytics.App;
using GSBC.ChurchApp.Features.Chats.App;
using GSBC.ChurchApp.Features.Configuration.App;
using GSBC.ChurchApp.Features.Events.App;
using GSBC.ChurchApp.Features.Feedback.App;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Features.Media.Client;
using GSBC.ChurchApp.Features.Ministries.App;
using GSBC.ChurchApp.Features.Notifications.App;
using GSBC.ChurchApp.Features.People.App;
using GSBC.ChurchApp.Features.News.App;
using GSBC.ChurchApp.Features.Prayer.App;
using GSBC.ChurchApp.Features.ReleaseNotes.App;
using GSBC.ChurchApp.Features.Rosters.App;
using GSBC.ChurchApp.Features.Sermons.App;
using GSBC.ChurchApp.Hosts.App;
using GSBC.ChurchApp.Platform.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Transport;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Icons;
using GSBC.ChurchApp.Platform.Ui;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

ClientModules modules = new(typeof(Program).Assembly);
modules.Add(new ClientModule("Home", typeof(Program).Assembly, [new NavEntry("Home", "", AppIcons.Home)]));

builder.Services
    .AddSingleton(modules)
    .AddPlatformClient()
    .AddPlatformUi()
    .AddNativeDataLayer(new Uri(builder.Configuration["Gateway"] ?? builder.HostEnvironment.BaseAddress))
    .AddAccountsApp(modules)
    .AddAccessOnPhone(modules)
    .AddPeopleApp(modules)
    .AddMinistriesApp(modules)
    .AddNewsApp(modules)
    .AddPrayerApp(modules)
    .AddRostersApp(modules)
    .AddChatsApp(modules)
    .AddConfigurationApp(modules)
    .AddMediaClient()
    .AddNotificationsApp(modules)
    .AddSermonsApp(modules)
    .AddEventsApp(modules);
modules.Add(new ClientModule("Settings", typeof(Program).Assembly,
    [new NavEntry("Appearance", "appearance", AppIcons.Appearance), new NavEntry("Storage", "storage", AppIcons.Storage)]));
modules.Add(new ClientModule("Design", typeof(Program).Assembly, [new NavEntry("Design gallery", "gallery", AppIcons.DesignGallery, FeedbackPermissions.Send)]));
builder.Services.AddReleaseNotesApp(modules);
builder.Services.AddAnalyticsApp(modules);
builder.Services.AddFeedbackApp(modules);

WebAssemblyHost host = builder.Build();
NativeBridge bridge = await host.Services.InitialiseNativeAsync();
if (bridge.IsDevelopmentBuild)
    modules.Add(new ClientModule("Development", typeof(Program).Assembly, [], [DevelopmentFeeds.Echo]));
await host.Services.StartNativeDataLayerAsync(modules);
await host.Services.RunClientStartupTasksAsync();
await host.RunAsync();
