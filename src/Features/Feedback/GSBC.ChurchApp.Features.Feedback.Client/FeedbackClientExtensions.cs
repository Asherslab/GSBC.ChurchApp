using GSBC.ChurchApp.Platform.Client.Modules;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.Client;

public static class FeedbackClientExtensions
{
    public static IServiceCollection AddFeedbackOnPhone(this IServiceCollection services) =>
        services.AddSingleton<RecentPages>().AddSingleton<IClientStartupTask>(provider => provider.GetRequiredService<RecentPages>())
            .AddSingleton<PhoneState>().AddSingleton<FeedbackOnPhone>();
}
