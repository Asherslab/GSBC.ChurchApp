using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Features.Analytics.Api.Data;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Media.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Api.Data;
using GSBC.ChurchApp.Features.Sermons.Import.Api.Data;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSyncData();
builder.Services.AddEmailData();
builder.Services.AddAccountsData();
builder.Services.AddAccessData();
builder.Services.AddConfigurationData();
builder.Services.AddMediaData();
builder.Services.AddAnalyticsData();
builder.Services.AddNotificationsData();
builder.Services.AddPeopleData();
builder.Services.AddSermonsData();
builder.Services.AddSermonsImportData();
builder.Services.AddEventsData();

using IHost host = builder.Build();
await using AsyncServiceScope scope = host.Services.CreateAsyncScope();

foreach (MigrationTarget target in scope.ServiceProvider.GetServices<MigrationTarget>().OrderBy(target => target.Order))
{
    DbContext context = (DbContext)scope.ServiceProvider.GetRequiredService(target.ContextType);
    await context.Database.MigrateAsync();
}
