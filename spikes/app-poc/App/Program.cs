using Gsbc.AppPoc;
using Gsbc.AppPoc.Data;
using Gsbc.AppPoc.Data.Outreach;
using Gsbc.AppPoc.Native;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<NativeBridge>();
builder.Services.AddSingleton<AppSettings>();
builder.Services.AddSingleton<ChurchClock>();
builder.Services.AddSingleton(SampleContent.Create());
builder.Services.AddSingleton(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<SermonLibrary>();
builder.Services.AddSingleton<WeekPlan>();
builder.Services.AddSingleton<PlayerService>();
builder.Services.AddSingleton<SiteMapSvg>();
builder.Services.AddSingleton<LifeGroupsService>();
builder.Services.AddSingleton<SitePlan>();
builder.Services.AddSingleton<OutreachService>();
builder.Services.AddSingleton<IMapLayer>(sp => sp.GetRequiredService<LifeGroupsService>());
builder.Services.AddSingleton<IMapLayer>(sp => sp.GetRequiredService<SitePlan>());

var host = builder.Build();
await host.Services.GetRequiredService<AppSettings>().LoadAsync();
await host.RunAsync();
