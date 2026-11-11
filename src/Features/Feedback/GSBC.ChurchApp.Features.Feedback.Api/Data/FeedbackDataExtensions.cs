using GSBC.ChurchApp.Platform.Data;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.Api.Data;

public static class FeedbackDataExtensions
{
    public static IServiceCollection AddFeedbackData(this IServiceCollection services) =>
        services.AddFeatureDbContext<FeedbackDbContext>(FeedbackDbContext.SchemaName, migrationOrder: 85);
}
