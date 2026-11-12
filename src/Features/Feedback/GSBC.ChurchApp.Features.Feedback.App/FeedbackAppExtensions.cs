using GSBC.ChurchApp.Features.Feedback.Client;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.App;

public static class FeedbackAppExtensions
{
    public static IServiceCollection AddFeedbackApp(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Feedback", typeof(FeedbackAppExtensions).Assembly,
            [new NavEntry("Send feedback", FeedbackDestinations.Send.TrimStart('/'), AppIcons.Feedback, FeedbackPermissions.Send)], [], []));
        return services.AddFeedbackOnPhone();
    }
}
