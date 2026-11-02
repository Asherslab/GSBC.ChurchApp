using GSBC.ChurchApp.Features.Access.Admin;
using GSBC.ChurchApp.Features.Accounts.Admin;
using GSBC.ChurchApp.Features.Media.Admin;
using GSBC.ChurchApp.Hosts.Admin;
using GSBC.ChurchApp.Platform.Client;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Transport;
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
    .AddGrpcWebTransport(new Uri(builder.HostEnvironment.BaseAddress))
    .AddAccountsAdmin(modules)
    .AddAccessAdmin(modules)
    .AddMediaAdmin(modules);

WebAssemblyHost host = builder.Build();
await host.Services.RunClientStartupTasksAsync();
await host.RunAsync();
