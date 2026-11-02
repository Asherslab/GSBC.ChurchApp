using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class SurfaceLayoutTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly NumberSetting Count = new("count", "How many", 3, 1, 10);
    private static readonly Surface Sermons = new("hub.sermons", HubSurfaces.Area, "Sermons", "", [Count], defaultOrder: 10);
    private static readonly Surface Events = new("hub.events", HubSurfaces.Area, "Events", "", [], defaultOrder: 20);
    private static readonly Surface Prayer = new("hub.prayer", HubSurfaces.Area, "Prayer", "", [], shownByDefault: false, defaultOrder: 30);
    private static readonly Surface[] Known = [Sermons, Events, Prayer, HubSurfaces.LinkTile.Surface];

    [Fact]
    public void ThePhoneMergesDefaultsWithWhatStaffPlacedAndIgnoresSurfacesItDoesNotKnow()
    {
        StoredPlacement[] stored =
        [
            Placed("1", Events, order: 5),
            Placed("2", HubSurfaces.LinkTile.Surface, order: 15),
            new("3", "hub.from-a-newer-app", 0, false, "", null, null, new Dictionary<string, string>(), 1),
        ];

        IReadOnlyList<ArrangedSurface> shown = SurfaceLayout.ShownTo(Known, stored, Viewer.Anonymous, Now);

        Assert.Equal(["hub.events", "hub.sermons", "hub.link"], shown.Select(entry => entry.Surface.Key));
    }

    [Fact]
    public void HiddenOutOfDateAndOtherPeoplesSurfacesAreLeftOut()
    {
        StoredPlacement[] stored =
        [
            Placed("1", Sermons, hidden: true),
            Placed("2", Events, endsAt: Now),
            Placed("3", Prayer, hidden: false, audience: Audience.Holding("prayer.inbox.read").Text),
            Placed("4", HubSurfaces.LinkTile.Surface, startsAt: Now.AddDays(1)),
        ];
        Viewer pastor = new(SignedIn: true, Holds: node => node == "prayer.inbox.read");

        Assert.Empty(SurfaceLayout.ShownTo(Known, stored, new Viewer(SignedIn: true, Holds: _ => false), Now));
        Assert.Equal(["hub.prayer"], SurfaceLayout.ShownTo(Known, stored, pastor, Now).Select(entry => entry.Surface.Key));
    }

    [Fact]
    public void SettingsReadTypedAndFallBackToTheirDefault()
    {
        ArrangedSurface valid = Arranged(new Dictionary<string, string> { [Count.Key] = "5" });
        ArrangedSurface outOfRange = Arranged(new Dictionary<string, string> { [Count.Key] = "50" });
        ArrangedSurface unset = Arranged(new Dictionary<string, string>());

        Assert.Equal(5, valid.Values.Get(Count));
        Assert.Equal(3, outOfRange.Values.Get(Count));
        Assert.Equal(3, unset.Values.Get(Count));
    }

    private static ArrangedSurface Arranged(Dictionary<string, string> values) =>
        SurfaceLayout.Arrange([Sermons], [new StoredPlacement("1", Sermons.Key, 0, false, "", null, null, values, 1)]).Single();

    private static StoredPlacement Placed(
        string id, Surface surface, int order = 0, bool hidden = false, string audience = "", DateTime? startsAt = null, DateTime? endsAt = null) =>
        new(id, surface.Key, order, hidden, audience, startsAt, endsAt, new Dictionary<string, string>(), 1);
}
