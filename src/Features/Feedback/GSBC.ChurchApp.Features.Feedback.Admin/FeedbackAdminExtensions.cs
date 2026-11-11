using GSBC.ChurchApp.Features.Feedback.Client;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.Admin;

public static class FeedbackAdminExtensions
{
    public static IServiceCollection AddFeedbackAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Feedback", typeof(FeedbackAdminExtensions).Assembly,
            [new NavEntry("Feedback", FeedbackDestinations.Desk.TrimStart('/'), "message-square", FeedbackPermissions.Triage, Area: NavAreas.Technical,
                Summary: "Problems and ideas people send from the app, with their screenshots and videos")],
            [FeedbackFeeds.Desk], []));
        return services.AddSingleton<FeedbackDeskClient>();
    }
}
