using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

public sealed class DraftRulesTests
{
    private static readonly string Title = HubSurfaces.LinkTile.Title.Key;
    private static readonly DraftRecord Christmas = new() { Id = Guid.CreateVersion7(), Name = "Christmas 2026" };
    private readonly DraftRules _rules = new(TestSurfaces.Catalog);

    [Fact]
    public void AFieldChangedForEveryoneSinceItWasEditedBlocksPublishingUntilSettled()
    {
        StoredPlacement edited = Tile("bible", revision: 1, "Bible");
        PlacementPatch renamed = new(edited.Id, HubSurfaces.Area.Key, null, edited.Surface, PatchKind.Change,
            Values: new Dictionary<string, string?> { [Title] = "Read the Bible" }, Base: edited);
        StoredPlacement live = Tile("bible", revision: 2, "Bible app");

        RpcException refused = Assert.Throws<RpcException>(() => _rules.RefuseChangedForEveryone([live], [renamed]));
        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
        Assert.Contains("\"Read the Bible\"", refused.Status.Detail);

        PlacementPatch mine = Assert.IsType<PlacementPatch>(DraftRules.Settled(renamed, live, keepMine: true));
        _rules.RefuseChangedForEveryone([live], [mine]);
        Assert.Equal("Read the Bible", mine.ApplyTo(live).Values[Title]);
        Assert.Null(DraftRules.Settled(renamed, live, keepMine: false));
    }

    [Fact]
    public void ASavedTileWhoseNameIsTakenWhateverItsCaseOrSpacingIsRefused()
    {
        StoredPlacement ministries = SavedTile("ministries", "Ministries");

        RpcException refused = Assert.Throws<RpcException>(() =>
            _rules.RefuseClashingNames(HubSurfaces.Area, [ministries, SavedTile("new", "  ministries ")], ["new"]));
        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
        Assert.Contains("“ministries”", refused.Status.Detail);

        _rules.RefuseClashingNames(HubSurfaces.Area, [ministries, SavedTile("new", "Ministries (wide, picture)")], ["new"]);
    }

    [Fact]
    public void AskingToPublishTellsThoseWhoPublishTheFirstAreaItTouches()
    {
        Notification waiting = _rules.WaitingToPublish(Christmas, [ChangeIn(HubSurfaces.Area.Key), ChangeIn(AppearanceSurfaces.Area.Key)], "Sam")!;

        Assert.Equal(DraftNotifications.WaitingToPublish, waiting.Kind);
        Assert.Equal(NotificationAudience.Holding(AppearanceSurfaces.Publish), waiting.Audience);
        Assert.Equal(($"/previews?draft={Christmas.Id}", $"draft/{Christmas.Id}"), (waiting.Link, waiting.Subject));
    }

    [Fact]
    public void SharingADraftTellsOnlyThatPersonWhereToPreviewIt()
    {
        Guid pastor = Guid.CreateVersion7();

        Notification shared = DraftRules.SharedWith(Christmas, pastor, "Sam");

        Assert.Equal((DraftNotifications.SharedWithYou, NotificationAudience.Person(pastor), $"/previews?draft={Christmas.Id}"),
            (shared.Kind, shared.Audience, shared.Link));
    }

    private static StoredPlacement Tile(string id, long revision, string title) =>
        new(id, HubSurfaces.LinkTile.Surface.Key, 10, false, "", null, null, new Dictionary<string, string> { [Title] = title }, revision);

    private static StoredPlacement SavedTile(string id, string name) =>
        new(id, HubSurfaces.Saved.Surface.Key, 0, false, "", null, null, new Dictionary<string, string> { [HubSurfaces.Saved.Name.Key] = name }, 1);

    private static DraftChangeRecord ChangeIn(string area) =>
        new() { Area = area, Surface = "", Patch = "", DraftId = Christmas.Id, PlacementId = Guid.CreateVersion7() };
}
