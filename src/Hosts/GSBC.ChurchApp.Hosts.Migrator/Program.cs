using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSyncData();
builder.Services.AddAccountsData();

using IHost host = builder.Build();
await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

foreach (MigrationTarget target in scope.ServiceProvider.GetServices<MigrationTarget>().OrderBy(target => target.Order))
{
    DbContext context = (DbContext)scope.ServiceProvider.GetRequiredService(target.ContextType);
    await context.Database.MigrateAsync();
}
