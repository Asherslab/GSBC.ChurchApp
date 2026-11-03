using GSBC.ChurchApp.Features.Access.Admin;
using GSBC.ChurchApp.Features.Accounts.Admin;
using GSBC.ChurchApp.Features.Configuration.Admin;
using GSBC.ChurchApp.Features.Media.Admin;
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
modules.Add(new ClientModule("Home", typeof(Program).Assembly, [new NavEntry("Home", "", "home")]));

builder.Services
    .AddSingleton(modules)
    .AddPlatformClient()
    .AddPlatformUi()
    .AddGrpcWebTransport(new Uri(builder.HostEnvironment.BaseAddress))
    .AddConfigurationAdmin(modules)
    .AddMediaAdmin(modules)
    .AddAccountsAdmin(modules)
    .AddAccessAdmin(modules);

WebAssemblyHost host = builder.Build();
await host.Services.RunClientStartupTasksAsync();
await host.RunAsync();
