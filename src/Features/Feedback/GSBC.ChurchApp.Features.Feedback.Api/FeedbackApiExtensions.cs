using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.Api;

public static class FeedbackApiExtensions
{
    public static IServiceCollection AddFeedbackApi(this IServiceCollection services)
    {
        services.AddFeedbackData();
        services.AddScoped<FeedbackSenders>().AddScoped<FeedbackSending>().AddScoped<FeedbackDesk>().AddScoped<FeedbackMedia>()
            .AddScoped<FeedbackTelling>().AddScoped<FeedbackKeeping>().AddScoped<HandledRequests>();
        services.AddScoped<IPersonData, FeedbackPersonData>();
        services.AddScoped<IMediaAreaAccess, FeedbackAttachmentAccess>();
        services.AddScoped<IMediaAreaAccess, FeedbackPhoneDataAccess>();
        services.AddMediaSystemArea(FeedbackMedia.Attachments);
        services.AddMediaSystemArea(FeedbackMedia.PhoneData);
        services.AddHostedService<FeedbackSchedule>();
        services.AddHostedService<SampleFeedback>();
        services.AddPermissions(FeedbackPermissions.All);
        services.AddNotificationKinds(FeedbackNotifications.All);

        services.AddSystemRole(new SystemRole(FeedbackPermissions.SenderRole, "Feedback",
            "Shows Send feedback in More on any phone they sign in on, for reporting problems and ideas while they test the app", ScopeKind.Global,
            [FeedbackPermissions.SendNode], Feature: RoleFeatures.InsightsAndHealth));
        services.AddSystemRole(new SystemRole(FeedbackPermissions.TriageRole, "Feedback Triage",
            "Looks after the feedback people send: reads it with its pictures and videos, moves it along, keeps notes and blocks a phone that misuses it",
            ScopeKind.Global, [FeedbackPermissions.SendNode, FeedbackPermissions.TriageNode], Feature: RoleFeatures.InsightsAndHealth));
        services.AddSystemRole(new SystemRole(FeedbackPermissions.PhoneDataRole, "Feedback Triage, with phone data",
            "Everything Feedback Triage does, and downloads the copy of a phone's stored data that comes with feedback, which can hold that person's private things. For whoever builds the app",
            ScopeKind.Global, ["feedback.*"], Feature: RoleFeatures.InsightsAndHealth));

        services.AddFeed(new FeedDefinition(FeedbackFeeds.Desk, FeedAudience.Holding(FeedbackPermissions.Triage)));
        return services;
    }

    public static IEndpointRouteBuilder MapFeedbackApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<FeedbackService>();
        endpoints.MapGrpcService<FeedbackDeskService>();
        return endpoints;
    }
}
