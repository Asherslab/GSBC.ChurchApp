using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Client.Preferences;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.ReleaseNotes.App;

public static class ReleaseNotesAppExtensions
{
    public static IServiceCollection AddReleaseNotesApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("ReleaseNotes", typeof(ReleaseNotesAppExtensions).Assembly,
            [new NavEntry("What's new", "whats-new", "sparkles")]));
        return services
            .AddSingleton(provider => new WhatsNew(provider.GetRequiredService<ILocalPreferences>(), BundledReleaseNotes.All))
            .AddSingleton<IOpeningSheet>(provider => provider.GetRequiredService<WhatsNew>());
    }
}
