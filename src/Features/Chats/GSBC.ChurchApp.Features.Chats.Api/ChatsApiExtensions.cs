using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Hosting;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Api.Sync;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Chats.Api;

public static class ChatsApiExtensions
{

    public static IServiceCollection AddChatsApi(this IServiceCollection services)
    {
        services.AddChatsData();
        services.AddScoped<ChatFacts>().AddScoped<ChatPublisher>().AddScoped<ChatNotifier>().AddScoped<ChatAudit>().AddScoped<OversightMarks>()
            .AddScoped<ChatActions>().AddScoped<ChatOversight>().AddScoped<ChatMedia>();
        services.AddScoped<IMediaAreaAccess, ChatMediaAccess>();
        services.AddMediaSystemArea(ChatMedia.Area);
        services.AddScoped<IScopeObjects, ChatScopeObjects>();
        services.AddScoped<IPersonData, ChatsPersonData>();
        services.AddHostedService<MembershipWatcher>();
        services.AddPermissions(ChatsPermissions.All);
        services.AddNotificationKinds(ChatsNotifications.All);

        services.AddSystemRole(new SystemRole(ChatsPermissions.MemberRole, "Chat member",
            "In this chat because a leader chose them, or because they asked to join its ministry", ScopeKind.Chat,
            [ChatsPermissions.MemberNode], AssignableBy: [], Hidden: true));
        services.AddSystemRole(new SystemRole(ChatsPermissions.AuditorRole, "Chat auditor",
            "Checks any chat in the church, gets reports sent straight to the auditors, and can pause someone from chats. Every check is logged",
            ScopeKind.Global, [ChatsPermissions.AuditNode, ChatsPermissions.CheckNode], Feature: RoleFeatures.Ministries));
        services.AddSystemRole(new SystemRole(ChatsPermissions.PausedRole, "Paused from chats",
            "Can't read or write in any chat until a chat auditor lets them back in", ScopeKind.Global, [], AssignableBy: [], Hidden: true));

        services.AddFeed(new FeedDefinition(ChatsFeeds.Chats, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(ChatsFeeds.Messages, FeedAudience.OwnRecords));
        services.AddFeed(new FeedDefinition(ChatsFeeds.Oversight, FeedAudience.EachRow));
        return services;
    }

    public static IEndpointRouteBuilder MapChatsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<ChatsService>();
        endpoints.MapGrpcService<ChatOversightService>();
        return endpoints;
    }
}
