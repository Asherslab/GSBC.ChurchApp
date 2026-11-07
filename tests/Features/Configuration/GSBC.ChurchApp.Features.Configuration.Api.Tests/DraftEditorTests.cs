using GSBC.ChurchApp.Platform.Api.Notifications;
using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

public sealed class DraftEditorTests : ConfigurationHost
{
    private static readonly string Title = HubSurfaces.LinkTile.Title.Key;
    private static readonly string Subtitle = HubSurfaces.LinkTile.Subtitle.Key;

    [Fact]
    public async Task PublishingKeepsAFixMadeForEveryoneAfterTheDraftStarted()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Placement bible = Assert.Single(await PublishNowAsync(staff, HubSurfaces.Area, null, LinkTile("Bible")));
        Draft conference = await NewDraftAsync(staff, "Conference 2027");
        await SaveAsync(staff, conference, Change(bible, (Title, "Read the Bible")));

        await PublishNowAsync(staff, HubSurfaces.Area, null, Change(bible, (Subtitle, "Every day")));
        await PublishAsync(staff, conference);

        Placement live = Assert.Single(await LiveAsync(staff, HubSurfaces.Area));
        Assert.Equal(("Read the Bible", "Every day"), (ValueOf(live, Title), ValueOf(live, Subtitle)));
    }

    [Fact]
    public async Task AFieldChangedForEveryoneSinceItWasEditedBlocksPublishingUntilSettled()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Placement bible = Assert.Single(await PublishNowAsync(staff, HubSurfaces.Area, null, LinkTile("Bible")));
        Draft draft = await NewDraftAsync(staff);
        await SaveAsync(staff, draft, Change(bible, (Title, "Read the Bible")));
        await PublishNowAsync(staff, HubSurfaces.Area, null, Change(bible, (Title, "Bible app")));

        RpcException refused = await Refused(() => PublishAsync(staff, draft));
        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
        Assert.Contains("\"Read the Bible\"", refused.Status.Detail);

        await Drafts(editor => editor.SettleAsync(staff, new SettleRequest { DraftId = draft.Id, PlacementId = bible.Id, KeepMine = true }, Cancel));
        await PublishAsync(staff, draft);
        Assert.Equal("Read the Bible", ValueOf(Assert.Single(await LiveAsync(staff, HubSurfaces.Area)), Title));
    }

    [Fact]
    public async Task TheLibraryKeepsOneSavedTilePerNameWhateverItsCaseOrSpacing()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        await PublishNowAsync(staff, HubSurfaces.Area, null, SavedTile("Ministries"));

        Draft draft = await NewDraftAsync(staff);
        RpcException refused = await Refused(() => SaveAsync(staff, draft, SavedTile("  ministries ")));
        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
        Assert.Contains("“ministries”", refused.Status.Detail);

        await SaveAsync(staff, draft, SavedTile("Ministries (wide, picture)"));

        Draft other = await NewDraftAsync(staff, "Another");
        await SaveAsync(staff, other, SavedTile("Youth"));
        Draft clashing = await NewDraftAsync(staff, "A third");
        await SaveAsync(staff, clashing, SavedTile("YOUTH"));
        await PublishAsync(staff, other);
        Assert.Equal(StatusCode.InvalidArgument, (await Refused(() => PublishAsync(staff, clashing))).StatusCode);
    }

    [Fact]
    public async Task PublishingNeedsThePublishPermissionForEveryAreaItTouches()
    {
        ClaimsPrincipal editor = Access.Person(ArrangesHub, (AppearanceSurfaces.EditNode, null), PublishesHub);
        Draft draft = await NewDraftAsync(editor);
        await SaveAsync(editor, draft, LinkTile("Camp"), Add(AppearanceSurfaces.Theme.Surface, AppearanceSurfaces.Area, null,
            (AppearanceSurfaces.Theme.Palette.Key, AppearanceSurfaces.Year2026)));

        Assert.Equal(StatusCode.PermissionDenied, (await Refused(() => PublishAsync(editor, draft))).StatusCode);
        Assert.Empty(await LiveAsync(editor, HubSurfaces.Area));

        Draft asked = await Drafts(drafts => drafts.AskToPublishAsync(editor, new DraftQuery { DraftId = draft.Id }, Cancel));
        Assert.Equal(DraftStatus.WaitingToPublish, asked.Status);

        Notification waiting = Assert.Single(Raised.All);
        Assert.Equal(DraftNotifications.WaitingToPublish, waiting.Kind);
        Assert.Equal(NotificationAudience.Holding(AppearanceSurfaces.Publish), waiting.Audience);
        Assert.Equal($"/previews?draft={draft.Id}", waiting.Link);

        await Drafts(drafts => drafts.DeleteAsync(editor, new DraftQuery { DraftId = draft.Id }, Cancel));
        Assert.Equal([$"draft/{draft.Id}"], Raised.Settled);
    }

    [Fact]
    public async Task TurningTheEventLayerOffLeavesWhatEveryoneSeesWithItsLaterChanges()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Placement map = Assert.Single(await PublishNowAsync(staff, HubSurfaces.Area, null, LinkTile("Site map")));
        Draft conference = await NewDraftAsync(staff, "Conference 2027");
        await SaveAsync(staff, conference, Change(map, (Title, "Conference site map")));
        DateTime starts = Clock.UtcNow.AddDays(10);

        await Drafts(editor => editor.TurnOnAsync(staff, new EventLayerRequest { DraftId = conference.Id, StartsAt = starts, EndsAt = starts.AddDays(3) }, Cancel));
        EventLayerMessage sent = ConfigurationContractsModel.Instance.Deserialize<EventLayerMessage>(new MemoryStream(LayerRecords(await FeedAsync(ConfigurationFeeds.Placements))[^1].Payload));
        Assert.Equal((conference.Id, starts), (sent.DraftId, sent.StartsAt!.Value), new LayerComparer());

        await PublishNowAsync(staff, HubSurfaces.Area, null, Change(map, (Subtitle, "Gates open at 8")));
        await Drafts(editor => editor.TurnOffAsync(staff, new DraftQuery { DraftId = conference.Id }, Cancel));

        Placement live = Assert.Single(await LiveAsync(staff, HubSurfaces.Area));
        Assert.Equal(("Site map", "Gates open at 8"), (ValueOf(live, Title), ValueOf(live, Subtitle)));
        Assert.True(LayerRecords(await FeedAsync(ConfigurationFeeds.Placements))[^1].Deleted);
        Assert.Single((await Drafts(editor => editor.ListAsync(staff, Cancel))).Drafts, draft => draft.Id == conference.Id && !draft.LayerOn);
    }

    [Fact]
    public async Task OnlyPeopleADraftIsSharedWithReceiveItAndUnsharingTakesItOffTheirPhone()
    {
        ClaimsPrincipal author = Access.Person(ArrangesHub);
        ClaimsPrincipal pastor = Access.Person();
        ClaimsPrincipal member = Access.Person();
        ClaimsPrincipal publisher = Access.Person(SeesEveryDraft);
        Draft draft = await NewDraftAsync(author);
        await SaveAsync(author, draft, LinkTile("Christmas"));
        await Drafts(editor => editor.ShareAsync(author, new ShareRequest { DraftId = draft.Id, UserId = FakeAccess.IdOf(pastor).ToString() }, sharing: true, Cancel));
        Notification shared = Assert.Single(Raised.All);
        Assert.Equal((DraftNotifications.SharedWithYou, NotificationAudience.Person(FakeAccess.IdOf(pastor)), $"/previews?draft={draft.Id}"),
            (shared.Kind, shared.Audience, shared.Link));

        FeedBatch pastorsPhone = Assert.Single((await PullAsync(pastor, ConfigurationFeeds.Drafts, 0)).Feeds);
        Assert.Equal([draft.Id], pastorsPhone.Changes.Select(change => change.Id));
        Assert.Equal([draft.Id], Assert.Single((await PullAsync(publisher, ConfigurationFeeds.Drafts, 0)).Feeds).Changes.Select(change => change.Id));
        Assert.Empty(Assert.Single((await PullAsync(member, ConfigurationFeeds.Drafts, 0)).Feeds).Changes);

        await Drafts(editor => editor.ShareAsync(author, new ShareRequest { DraftId = draft.Id, UserId = FakeAccess.IdOf(pastor).ToString() }, sharing: false, Cancel));

        FeedBatch afterUnsharing = Assert.Single((await PullAsync(pastor, ConfigurationFeeds.Drafts, pastorsPhone.Cursor)).Feeds);
        Assert.True(afterUnsharing.Reset);
        Assert.Empty(Assert.Single((await PullAsync(pastor, ConfigurationFeeds.Drafts, 0)).Feeds).Changes);
    }

    [Fact]
    public async Task APictureUsedByADraftStaysInUseUntilTheDraftIsDeleted()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub);
        MediaRef photo = Media.Add(new MediaRef { Id = "photo", Key = $"{new string('a', 64)}.jpg", ContentType = "image/jpeg", Url = "https://media.example/a.jpg" });
        PatchMessage tile = LinkTile("Camp");
        tile.Values.Add(new ValueChange { Key = HubSurfaces.LinkTile.Image.Key, Value = HubSurfaces.LinkTile.Image.Format(photo) });
        Draft draft = await NewDraftAsync(staff);

        await SaveAsync(staff, draft, tile);
        Assert.Equal([photo.Id], Media.UsedBy($"config.draft/{draft.Id}"));

        await Drafts(editor => editor.DeleteAsync(staff, new DraftQuery { DraftId = draft.Id }, Cancel));
        Assert.Empty(Media.UsedBy($"config.draft/{draft.Id}"));
    }

    private static List<FeedChangeRecord> LayerRecords(List<FeedChangeRecord> feed) =>
        feed.Where(change => change.RecordId == ConfigurationFeeds.EventLayerRecord).ToList();

    private sealed class LayerComparer : IEqualityComparer<(string, DateTime)>
    {
        public bool Equals((string, DateTime) left, (string, DateTime) right) =>
            left.Item1 == right.Item1 && Math.Abs((left.Item2 - right.Item2).TotalMilliseconds) < 1;

        public int GetHashCode((string, DateTime) value) => value.Item1.GetHashCode();
    }

    private static PatchMessage SavedTile(string name) =>
        Add(HubSurfaces.Saved.Surface, HubSurfaces.Area, null, (HubSurfaces.Saved.Name.Key, name), (HubSurfaces.Saved.Kind.Key, HubSurfaces.PlainTile.Surface.Key),
            (HubSurfaces.Saved.Tile.Key, HubSurfaces.Saved.Tile.Format(new Dictionary<string, string> { [HubTileKeys.Title] = name })));
}
