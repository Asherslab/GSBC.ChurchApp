using GSBC.ChurchApp.Features.Accounts.App;
using GSBC.ChurchApp.Features.Configuration.App;
using GSBC.ChurchApp.Features.Media.Client;
using GSBC.ChurchApp.Hosts.App;
using GSBC.ChurchApp.Platform.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Native;
using GSBC.ChurchApp.Platform.Client.Transport;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

ClientModules modules = new(typeof(Program).Assembly);
modules.Add(new ClientModule("Home", typeof(Program).Assembly, [new NavEntry("Home", "", "home")]));
modules.Add(new ClientModule("Storage", typeof(Program).Assembly, [new NavEntry("Storage", "storage", "database")]));

builder.Services
    .AddSingleton(modules)
    .AddPlatformClient()
    .AddNativeDataLayer(new Uri(builder.Configuration["Gateway"] ?? builder.HostEnvironment.BaseAddress))
    .AddAccountsApp(modules)
    .AddConfigurationApp(modules)
    .AddMediaClient();

WebAssemblyHost host = builder.Build();
NativeBridge bridge = await host.Services.InitialiseNativeAsync();
if (bridge.IsDevelopmentBuild)
    modules.Add(new ClientModule("Development", typeof(Program).Assembly, [], [DevelopmentFeeds.Echo]));
await host.Services.StartNativeDataLayerAsync(modules);
await host.RunAsync();
