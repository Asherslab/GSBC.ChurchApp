using GSBC.ChurchApp.Features.Access.Admin;
using GSBC.ChurchApp.Features.Analytics.Admin;
using GSBC.ChurchApp.Features.Accounts.Admin;
using GSBC.ChurchApp.Features.Configuration.Admin;
using GSBC.ChurchApp.Features.Media.Admin;
using GSBC.ChurchApp.Features.Notifications.Admin;
using GSBC.ChurchApp.Features.Events.Admin;
using GSBC.ChurchApp.Features.Sermons.Admin;
using GSBC.ChurchApp.Hosts.Admin;
using GSBC.ChurchApp.Platform.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
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
    .AddPlatformUi()
    .AddGrpcWebTransport(new Uri(builder.HostEnvironment.BaseAddress))
    .AddConfigurationAdmin(modules)
    .AddMediaAdmin(modules)
    .AddNotificationsAdmin(modules)
    .AddSermonsAdmin(modules)
    .AddEventsAdmin(modules)
    .AddAccountsAdmin(modules)
    .AddAccessAdmin(modules)
    .AddAnalyticsAdmin(modules);

WebAssemblyHost host = builder.Build();
await host.Services.RunClientStartupTasksAsync();
await host.RunAsync();
