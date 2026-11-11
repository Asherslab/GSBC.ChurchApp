extern alias ApiHost;

using System.Text.RegularExpressions;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class NotificationLinkRules
{
    [Fact]
    public void EveryNotificationOpensAPlaceOnThePhoneOrForAdminPanelWorkAPageThere()
    {
        using RoutesOnly<ApiHost::Program> api = new();
        DestinationCatalog places = api.Services.GetRequiredService<DestinationCatalog>();
        List<NotificationKind> kinds = api.Services.GetServices<NotificationKindSet>().SelectMany(set => set.Kinds).ToList();
        List<string> appRoutes = Routes(path => path.Contains("/Hosts/App/") || AppProject().IsMatch(path));
        List<string> adminRoutes = Routes(path => path.Contains("/Hosts/GSBC.ChurchApp.Hosts.Admin/") || AdminProject().IsMatch(path));

        List<string> problems = [];
        foreach (NotificationKind kind in kinds)
        {
            if (kind.Opens is not { } opens)
                problems.Add($"{kind.Key}: declares no Opens");
            else
                problems.AddRange(opens.Routes
                    .Where(template => !OpensOnThePhone(template, places, appRoutes) && !(kind.InAdminPanel && adminRoutes.Any(route => RouteTemplates.Match(template, route))))
                    .Select(template => $"{kind.Key}: {template}"));
        }

        Assert.NotEmpty(kinds);
        Assert.Empty(problems);
    }

    private static bool OpensOnThePhone(string template, DestinationCatalog places, List<string> appRoutes) =>
        places.Resolve(RouteTemplates.Sample(template)) is not null && appRoutes.Any(route => RouteTemplates.Match(template, route));

    private static List<string> Routes(Func<string, bool> inHost) =>
        RepoFiles.Under("src", ".razor")
            .Select(path => path.Replace(Path.DirectorySeparatorChar, '/'))
            .Where(inHost)
            .SelectMany(path => PageRoute().Matches(File.ReadAllText(path)).Select(match => match.Groups[1].Value))
            .ToList();

    [GeneratedRegex(@"/Features/[^/]+/[^/]+\.App/")]
    private static partial Regex AppProject();

    [GeneratedRegex(@"/Features/[^/]+/[^/]+\.(Admin|Client)/")]
    private static partial Regex AdminProject();

    [GeneratedRegex("@page \"([^\"]*)\"")]
    private static partial Regex PageRoute();
}
