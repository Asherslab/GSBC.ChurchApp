using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public interface IClientStartupTask
{
    Task RunAsync();
}

public static class ClientStartup
{
    public static async Task RunClientStartupTasksAsync(this IServiceProvider services)
    {
        foreach (IClientStartupTask task in services.GetServices<IClientStartupTask>())
            await task.RunAsync();
    }
}
