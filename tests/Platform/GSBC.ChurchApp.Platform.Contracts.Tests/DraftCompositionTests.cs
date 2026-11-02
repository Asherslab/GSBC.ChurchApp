using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class DraftCompositionTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
    private static readonly string Title = HubSurfaces.LinkTile.Title.Key;
    private static readonly string Subtitle = HubSurfaces.LinkTile.Subtitle.Key;

    [Fact]
    public void ADraftShowsLiveAsItIsNowPlusItsChangesIncludingAFixMadeAfterItStarted()
    {
        StoredPlacement started = Tile("bible", revision: 1, title: "Bible", subtitle: "Read it");
        PlacementPatch renamed = Renamed(started, "Read the Bible");
        PlacementPatch camp = new("camp", HubSurfaces.Area.Key, null, HubSurfaces.LinkTile.Surface.Key, PatchKind.Add,
            Order: 20, Values: new Dictionary<string, string?> { [Title] = "Camp" });
        StoredPlacement fixedInLive = Tile("bible", revision: 2, title: "Bible", subtitle: "Read it daily");

        IReadOnlyList<StoredPlacement> shown = DraftComposition.Compose([fixedInLive], null, [renamed, camp], Now);

        StoredPlacement bible = shown.Single(placement => placement.Id == "bible");
        Assert.Equal("Read the Bible", bible.Values[Title]);
        Assert.Equal("Read it daily", bible.Values[Subtitle]);
        Assert.Equal(["bible", "camp"], shown.Select(placement => placement.Id));
        Assert.Empty(DraftComposition.ChangedSince([fixedInLive], [renamed]));
    }

    [Fact]
    public void LiveChangingTheSameFieldIsReportedUntilItAgreesWithTheDraft()
    {
        StoredPlacement started = Tile("bible", revision: 1, title: "Bible");
        PlacementPatch renamed = Renamed(started, "Read the Bible");

        ChangedForEveryone changed = Assert.Single(DraftComposition.ChangedSince([Tile("bible", revision: 2, title: "Bible app")], [renamed]));
        Assert.Equal([PatchField.Value(Title)], changed.Fields);
        Assert.Empty(DraftComposition.ChangedSince([Tile("bible", revision: 3, title: "Read the Bible")], [renamed]));
        Assert.True(Assert.Single(DraftComposition.ChangedSince([], [renamed])).Removed);
    }

    [Fact]
    public void AScheduledEventLayerAppliesOnlyBetweenItsDates()
    {
        StoredPlacement live = Tile("map", revision: 1, title: "Site map", hidden: true);
        EventLayer conference = new("conference", "Conference 2027", Now.AddDays(1), Now.AddDays(3),
            [new PlacementPatch("map", HubSurfaces.Area.Key, null, live.Surface, PatchKind.Change, Hidden: false, Base: live)]);

        Assert.True(DraftComposition.Compose([live], conference, [], Now).Single().Hidden);
        Assert.False(DraftComposition.Compose([live], conference, [], Now.AddDays(2)).Single().Hidden);
        Assert.True(DraftComposition.Compose([live], conference, [], Now.AddDays(3)).Single().Hidden);
    }

    private static PlacementPatch Renamed(StoredPlacement placement, string title) =>
        new(placement.Id, HubSurfaces.Area.Key, null, placement.Surface, PatchKind.Change,
            Values: new Dictionary<string, string?> { [Title] = title }, Base: placement);

    private static StoredPlacement Tile(string id, long revision, string title, string subtitle = "", bool hidden = false) =>
        new(id, HubSurfaces.LinkTile.Surface.Key, 10, hidden, "", null, null,
            new Dictionary<string, string> { [Title] = title, [Subtitle] = subtitle }, revision);
}
