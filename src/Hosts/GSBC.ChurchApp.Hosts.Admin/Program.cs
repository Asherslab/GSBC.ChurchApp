using GSBC.ChurchApp.Features.Access.Admin;
using GSBC.ChurchApp.Features.Analytics.Admin;
using GSBC.ChurchApp.Features.Accounts.Admin;
using GSBC.ChurchApp.Features.Configuration.Admin;
using GSBC.ChurchApp.Features.Media.Admin;
using GSBC.ChurchApp.Features.Notifications.Admin;
using GSBC.ChurchApp.Features.Events.Admin;
using GSBC.ChurchApp.Features.Ministries.Admin;
using GSBC.ChurchApp.Features.Feedback.Admin;
using GSBC.ChurchApp.Features.Prayer.Admin;
using GSBC.ChurchApp.Features.Rosters.Admin;
using GSBC.ChurchApp.Features.Chats.Admin;
using GSBC.ChurchApp.Features.People.Admin;
using GSBC.ChurchApp.Features.Sermons.Admin;
using GSBC.ChurchApp.Features.Sermons.Import.Admin;
using GSBC.ChurchApp.Hosts.Admin;
using GSBC.ChurchApp.Platform.Client;
using GSBC.ChurchApp.Platform.Client.Destinations;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Online;
using GSBC.ChurchApp.Platform.Client.Outbox;
using GSBC.ChurchApp.Platform.Client.Transport;
using GSBC.ChurchApp.Platform.Ui;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

ClientModules modules = new(typeof(Program).Assembly);
modules.Add(new ClientModule("Home", typeof(Program).Assembly,
    [new NavEntry("Home", "", "home"), new NavEntry("Places in the app", "places", "link", Area: NavAreas.TheApp, Summary: "Every page and thing in the app, with its link")]));

builder.Services
    .AddSingleton(modules)
    .AddPlatformClient()
    .AddPlatformUi(desktop: true)
    .AddGrpcWebTransport(new Uri(builder.HostEnvironment.BaseAddress))
    .AddSingleton<DestinationSearching>()
    .AddSingleton<IWrites, DirectWrites>()
    .AddConfigurationAdmin(modules)
    .AddMediaAdmin(modules)
    .AddNotificationsAdmin(modules)
    .AddSermonsAdmin(modules)
    .AddSermonsImportAdmin(modules)
    .AddEventsAdmin(modules)
    .AddAccountsAdmin(modules)
    .AddAccessAdmin(modules)
    .AddPeopleAdmin(modules)
    .AddMinistriesAdmin(modules)
    .AddPrayerAdmin(modules)
    .AddFeedbackAdmin(modules)
    .AddRostersAdmin(modules)
    .AddChatsAdmin(modules)
    .AddAnalyticsAdmin(modules);

WebAssemblyHost host = builder.Build();
await host.Services.RunClientStartupTasksAsync();
await host.RunAsync();
