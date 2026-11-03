using Microsoft.Extensions.Configuration;

namespace GSBC.ChurchApp.Features.Analytics.Api;

internal sealed class AnalyticsEnvironment(IConfiguration configuration)
{
    private const string DevelopmentSite = "https://dev-app.baptist.com.au";

    public string SiteUrl { get; } = (configuration["Accounts:SiteUrl"] ?? DevelopmentSite).TrimEnd('/');

    public string Name => Uri.TryCreate(SiteUrl, UriKind.Absolute, out Uri? site) ? site.Host : SiteUrl;
}
