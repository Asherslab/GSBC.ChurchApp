using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class TabBarTests
{
    private static readonly DateTime Now = new(2026, 11, 8, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Viewer Visitor = Viewer.Anonymous;
    private static readonly Viewer Member = new(SignedIn: true, Holds: _ => false);

    [Fact]
    public void WithNothingPublishedThePhoneHasTheTabsItAlwaysHad()
    {
        Assert.Equal(["Home", "Sermons"], Words(For(Visitor)));
    }

    [Fact]
    public void ThePhoneDrawsHomeThenTheFirstThreeItsPersonSeesInOrder()
    {
        StoredPlacement[] stored =
        [
            Tab("calendar", "/week", "Calendar", order: 150),
            Tab("give", "/give", "Give", order: 300),
            Tab("groups", "/life-groups", "Groups", order: 400, audience: Audience.SignedIn),
            Tab("prayer", "/prayer", "Prayer", order: 500),
        ];

        Assert.Equal(["Home", "Calendar", "Sermons", "Give"], Words(For(Visitor, stored)));
        Assert.Equal(["Home", "Calendar", "Sermons", "Give"], Words(For(Member, stored)));
        Assert.Equal(["Home", "Calendar", "Sermons", "Prayer"], Words(For(Visitor, stored[0], stored[3])));
    }

    [Fact]
    public void ATabTakenOffTheBarOrForSomeoneElseLeavesNoGap()
    {
        StoredPlacement[] stored =
        [
            Hidden(TabBarSurfaces.Sermons),
            Tab("groups", "/life-groups", "Groups", order: 300, audience: Audience.SignedIn),
            Tab("visit", "/visit", "Visit us", order: 400, endsAt: Now),
        ];

        Assert.Equal(["Home"], Words(For(Visitor, stored)));
        Assert.Equal(["Home", "Groups"], Words(For(Member, stored)));
    }

    [Fact]
    public void ATabBarSavedWithoutHomeFirstStillHasHomeFirst()
    {
        StoredPlacement[] stored =
        [
            Hidden(TabBarSurfaces.Home),
            Tab("calendar", "/week", "Calendar", order: 10),
            Tab("again", "/", "Start", order: 20),
        ];

        Assert.Equal(["Home", "Calendar", "Sermons"], Words(For(Visitor, stored)));
    }

    [Fact]
    public void ThePlaceTwiceForOnePersonIsOneTab()
    {
        Assert.Equal(["Home", "Sermons"], Words(For(Visitor, [Tab("again", "/sermons", "Listen", order: 300)])));
    }

    private static IReadOnlyList<AppTab> For(Viewer viewer, params StoredPlacement[] stored) =>
        TabBar.For(SurfaceLayout.ShownTo(TabBarSurfaces.All.Surfaces, stored, viewer, Now));

    private static IEnumerable<string> Words(IReadOnlyList<AppTab> tabs) => tabs.Select(tab => tab.Word);

    private static StoredPlacement Tab(string id, string opens, string word, int order, Audience? audience = null, DateTime? endsAt = null) =>
        new(id, TabBarSurfaces.Tab.Key, order, false, (audience ?? Audience.Everyone).Text, null, endsAt,
            new Dictionary<string, string> { [TabSettings.Opens.Key] = opens, [TabSettings.Word.Key] = word, [TabSettings.Icon.Key] = "sparkles" }, 1);

    private static StoredPlacement Hidden(Surface starting) =>
        new(starting.Key, starting.Key, starting.DefaultOrder, true, "", null, null, new Dictionary<string, string>(), 1);
}
