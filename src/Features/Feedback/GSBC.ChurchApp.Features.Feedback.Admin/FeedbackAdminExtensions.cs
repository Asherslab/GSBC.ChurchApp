using GSBC.ChurchApp.Features.Feedback.Admin.Components;
using GSBC.ChurchApp.Features.Feedback.Client;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Icons;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.Admin;

public static class FeedbackAdminExtensions
{
    private static RenderFragment SendFeedback(SidebarSheet sheet) => builder =>
    {
        builder.OpenComponent<SendFeedbackSheet>(0);
        builder.AddComponentParameter(1, nameof(SendFeedbackSheet.Open), sheet.Open);
        builder.AddComponentParameter(2, nameof(SendFeedbackSheet.OpenChanged), sheet.OpenChanged);
        builder.CloseComponent();
    };

    public static IServiceCollection AddFeedbackAdmin(this IServiceCollection services, ClientModules modules)
    {
        modules.Add(new ClientModule("Feedback", typeof(FeedbackAdminExtensions).Assembly,
            [new NavEntry("Feedback", FeedbackDestinations.Desk.TrimStart('/'), AppIcons.Feedback, FeedbackPermissions.Triage, Area: NavAreas.Technical,
                Summary: "Problems and ideas people send from the app, with their screenshots and videos")],
            [FeedbackFeeds.Desk], []));
        return services.AddSingleton<FeedbackDeskClient>()
            .AddSingleton<RecentPages>().AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<RecentPages>())
            .AddSingleton<FeedbackFromAdmin>()
            .AddSingleton(new SidebarAction("Send feedback", AppIcons.Feedback, FeedbackPermissions.Send, SendFeedback));
    }
}
