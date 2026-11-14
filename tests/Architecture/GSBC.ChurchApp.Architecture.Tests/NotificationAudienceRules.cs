extern alias ApiHost;

using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed class NotificationAudienceRules
{
    private static readonly Dictionary<string, string> ForAnyone = new()
    {
        ["notifications.announcement"] = "church news and announcements: staff choose who, and it can be everyone",
        ["events.told"] = "news about an event goes to whoever has its calendar, and every calendar can be anyone's",
        ["events.digest.out"] = "the weekly digest goes to everyone",
        ["ministries.answered"] = "anyone may ask to join a ministry",
        ["people.family.asked"] = "anyone may be asked to be someone's parent; they aren't one until they say yes",
        ["people.family.linked"] = "anyone may have a parent linked to them",
        ["prayer.prayed"] = "anyone may send a prayer request",
        ["prayer.answered"] = "anyone may send a prayer request",
    };

    [Fact]
    public void EveryKindForPeopleHoldingAPermissionSaysWhichSoOthersDontSeeItInTheirSettings()
    {
        using RoutesOnly<ApiHost::Program> api = new();
        List<NotificationKind> kinds = Kinds(api);

        List<string> undeclared = kinds.Where(kind => kind.For is not { Count: > 0 } && !ForAnyone.ContainsKey(kind.Key)).Select(kind => kind.Key).ToList();
        List<string> declaredAndForAnyone = kinds.Where(kind => kind.For is { Count: > 0 } && ForAnyone.ContainsKey(kind.Key)).Select(kind => kind.Key).ToList();

        Assert.NotEmpty(kinds);
        Assert.Empty(undeclared);
        Assert.Empty(declaredAndForAnyone);
        Assert.Empty(ForAnyone.Keys.Except(kinds.Select(kind => kind.Key)));
    }

    [Fact]
    public void EveryPermissionAKindIsForExistsWithItsScope()
    {
        using RoutesOnly<ApiHost::Program> api = new();
        PermissionCatalog permissions = api.Services.GetRequiredService<PermissionCatalog>();

        List<string> unknown = Kinds(api)
            .SelectMany(kind => (kind.For ?? []).Select(permission => (kind.Key, permission)))
            .Where(pair => permissions.ByNode.GetValueOrDefault(pair.permission.Node) is not { } known || known.ScopeKind != pair.permission.ScopeKind)
            .Select(pair => $"{pair.Key}: {pair.permission.Node} ({pair.permission.ScopeKind.Name})")
            .ToList();

        Assert.Empty(unknown);
    }

    private static List<NotificationKind> Kinds(RoutesOnly<ApiHost::Program> api) =>
        api.Services.GetServices<NotificationKindSet>().SelectMany(set => set.Kinds).ToList();
}
