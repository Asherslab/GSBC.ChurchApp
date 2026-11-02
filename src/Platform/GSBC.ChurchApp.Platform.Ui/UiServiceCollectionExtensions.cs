using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GSBC.ChurchApp.Platform.Ui;

public static class UiServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformUi(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        return services.AddSingleton<Shell>().AddSingleton<Appearance>();
    }
}
