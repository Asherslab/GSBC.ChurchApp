using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GSBC.ChurchApp.Architecture.Tests;

internal sealed class RoutesOnly<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            foreach (ServiceDescriptor appWork in services.Where(IsTheAppsBackgroundWork).ToList())
                services.Remove(appWork);
        });

    private static bool IsTheAppsBackgroundWork(ServiceDescriptor service) =>
        service.ServiceType == typeof(IHostedService)
        && service.ImplementationType?.Namespace?.StartsWith("Microsoft.", StringComparison.Ordinal) != true;
}
