using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Chats.Api.Data;

public static class ChatsDataExtensions
{
    public static IServiceCollection AddChatsData(this IServiceCollection services) =>
        services.AddFeatureDbContext<ChatsDbContext>(ChatsDbContext.SchemaName, migrationOrder: 82);
}
