using GSBC.ChurchApp.Features.Access.Client;
using GSBC.ChurchApp.Features.Analytics.App;
using GSBC.ChurchApp.Features.Accounts.App;
using GSBC.ChurchApp.Features.Configuration.App;
using GSBC.ChurchApp.Features.Media.Client;
using GSBC.ChurchApp.Features.Notifications.App;
using GSBC.ChurchApp.Features.ReleaseNotes.App;
using GSBC.ChurchApp.Features.Events.App;
using GSBC.ChurchApp.Features.Ministries.App;
using GSBC.ChurchApp.Features.Prayer.App;
using GSBC.ChurchApp.Features.Rosters.App;
using GSBC.ChurchApp.Features.People.App;
using GSBC.ChurchApp.Features.Sermons.App;
using GSBC.ChurchApp.Hosts.App;
using GSBC.ChurchApp.Platform.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Transport;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Ui;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

ClientModules modules = new(typeof(Program).Assembly);
modules.Add(new ClientModule("Home", typeof(Program).Assembly, [new NavEntry("Home", "", "home")]));

builder.Services
    .AddSingleton(modules)
    .AddPlatformClient()
    .AddPlatformUi()
    .AddNativeDataLayer(new Uri(builder.Configuration["Gateway"] ?? builder.HostEnvironment.BaseAddress))
    .AddAccountsApp(modules)
    .AddAccessOnPhone(modules)
    .AddPeopleApp(modules)
    .AddMinistriesApp(modules)
    .AddPrayerApp(modules)
    .AddRostersApp(modules)
    .AddConfigurationApp(modules)
    .AddMediaClient()
    .AddNotificationsApp(modules)
    .AddSermonsApp(modules)
    .AddEventsApp(modules);
modules.Add(new ClientModule("Settings", typeof(Program).Assembly,
    [new NavEntry("Appearance", "appearance", "sun"), new NavEntry("Storage", "storage", "database")]));
builder.Services.AddReleaseNotesApp(modules);
builder.Services.AddAnalyticsApp(modules);

WebAssemblyHost host = builder.Build();
NativeBridge bridge = await host.Services.InitialiseNativeAsync();
if (bridge.IsDevelopmentBuild)
    modules.Add(new ClientModule("Development", typeof(Program).Assembly, [new NavEntry("Design gallery", "gallery", "palette")], [DevelopmentFeeds.Echo]));
await host.Services.StartNativeDataLayerAsync(modules);
await host.Services.RunClientStartupTasksAsync();
await host.RunAsync();
