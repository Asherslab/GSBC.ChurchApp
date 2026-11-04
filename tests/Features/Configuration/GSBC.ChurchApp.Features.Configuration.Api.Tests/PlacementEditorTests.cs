using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Data;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

public sealed class PlacementEditorTests : ConfigurationHost
{
    [Fact]
    public async Task OnlyHoldersOfTheAreasPermissionAtItsScopeCanArrangeIt()
    {
        ClaimsPrincipal leader = Access.Person((EditModules.Node, "42"));
        Draft draft = await NewDraftAsync(leader);

        await SaveAsync(leader, draft, Add(News, Modules, "42"));

        Assert.Equal(StatusCode.PermissionDenied, (await Refused(() => SaveAsync(leader, draft, Add(News, Modules, "7")))).StatusCode);
        Assert.Equal(StatusCode.PermissionDenied, (await Refused(() => SaveAsync(leader, draft, LinkTile("Bible")))).StatusCode);
    }

    [Fact]
    public async Task EveryValueIsCheckedAgainstItsDescriptorAndNothingInvalidIsStored()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub);
        Draft draft = await NewDraftAsync(staff);
        PatchMessage tile = LinkTile("Bible", link: "http://example.org");
        tile.Values.Add(new ValueChange { Key = "font", Value = "Comic Sans" });
        tile.Audience = "permission:nobody.declared.this";
        tile.SetsDates = true;
        tile.StartsAt = new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);
        tile.EndsAt = tile.StartsAt.Value.AddDays(-1);

        RpcException refused = await Refused(() => SaveAsync(staff, draft, tile));

        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
        Assert.Contains("https://", refused.Status.Detail);
        Assert.Contains("'font'", refused.Status.Detail);
        Assert.Contains("nobody.declared.this", refused.Status.Detail);
        Assert.Contains("end after it starts", refused.Status.Detail);
        Assert.Empty((await Drafts(editor => editor.GetAsync(staff, new DraftQuery { DraftId = draft.Id }, Cancel))).Changes);
    }

    [Fact]
    public async Task PublishingReachesPhonesThroughTheFeedAndARemovalDeletesThere()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Placement saved = Assert.Single(await PublishNowAsync(staff, HubSurfaces.Area, null, LinkTile("Bible")));

        await PublishNowAsync(staff, HubSurfaces.Area, null, Remove(saved));

        List<FeedChangeRecord> feed = await FeedAsync(ConfigurationFeeds.Placements);
        Assert.Equal([false, true], feed.Select(change => change.Deleted));
        Assert.All(feed, change => Assert.Equal(saved.Id, change.RecordId));
        Placement sent = ConfigurationContractsModel.Instance.Deserialize<Placement>(new MemoryStream(feed[0].Payload));
        Assert.Equal("Bible", ValueOf(sent, HubSurfaces.LinkTile.Title.Key));
        Assert.Equal(ConfigurationFeeds.GroupOf(HubSurfaces.Area.Key, null), feed[0].GroupKey);
    }

    [Fact]
    public async Task ASecondCopyOfASurfaceThatIsPlacedOnceIsRefused()
    {
        ClaimsPrincipal leader = Access.Person((EditModules.Node, "42"), (PublishModules.Node, "42"));
        await PublishNowAsync(leader, Modules, "42", Add(News, Modules, "42"));
        Draft draft = await NewDraftAsync(leader);

        Assert.Equal(StatusCode.Aborted, (await Refused(() => SaveAsync(leader, draft, Add(News, Modules, "42")))).StatusCode);
    }

    [Fact]
    public async Task UndoingAPublishedDraftRestoresWhatWasThereIsAuditedAndCanBePublishedAgain()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Placement bible = Assert.Single(await PublishNowAsync(staff, HubSurfaces.Area, null, LinkTile("Bible")));
        await PublishNowAsync(staff, HubSurfaces.Area, null, Change(bible, (HubSurfaces.LinkTile.Title.Key, "Read the Bible")), LinkTile("Camp"));

        ChangeList history = await Placements(editor => editor.ListChangesAsync(staff, new AreaQuery { Area = HubSurfaces.Area.Key }, Cancel));
        Assert.Equal("A quick fix", history.Batches[0].DraftName);
        await Placements(editor => editor.RollBackAsync(staff, new RollBackRequest { BatchId = history.Batches[0].Id }, Cancel));

        Placement restored = Assert.Single(await LiveAsync(staff, HubSurfaces.Area));
        Assert.Equal(bible.Id, restored.Id);
        Assert.Equal("Bible", ValueOf(restored, HubSurfaces.LinkTile.Title.Key));
        ChangeBatch undo = (await Placements(editor => editor.ListChangesAsync(staff, new AreaQuery { Area = HubSurfaces.Area.Key }, Cancel))).Batches[0];
        Assert.Equal(history.Batches[0].Id, undo.RollsBack);
        Assert.Equal(2, undo.Changes.Count);

        PublishedDraft undone = (await Drafts(editor => editor.ListAsync(staff, Cancel))).Published[0];
        Assert.Equal(undo.Id, undone.UndoneBatchId);
        await Placements(editor => editor.RollBackAsync(staff, new RollBackRequest { BatchId = undone.UndoneBatchId! }, Cancel));

        Assert.Equal(["Read the Bible", "Camp"], (await LiveAsync(staff, HubSurfaces.Area)).Select(tile => ValueOf(tile, HubSurfaces.LinkTile.Title.Key)).Order().Reverse());
        Assert.Null((await Drafts(editor => editor.ListAsync(staff, Cancel))).Published[0].UndoneBatchId);
    }

    [Fact]
    public async Task ImagesInSettingsAreTheLibrarysAndForgedOrPrivateOnesAreRefused()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        MediaRef photo = Media.Add(new MediaRef { Id = "photo", Key = $"{new string('a', 64)}.jpg", ContentType = "image/jpeg", Url = "https://media.example/a.jpg", Width = 800, Height = 600 });
        MediaRef notes = Media.Add(new MediaRef { Id = "notes", Key = $"{new string('b', 64)}.jpg", ContentType = "image/jpeg", IsPrivate = true });

        PatchMessage tile = LinkTile("Camp");
        tile.Values.Add(Value(HubSurfaces.LinkTile.Image, new MediaRef { Id = photo.Id, Key = photo.Key, ContentType = "image/jpeg", Url = "https://attacker.example/x.jpg" }));
        Placement saved = Assert.Single(await PublishNowAsync(staff, HubSurfaces.Area, null, tile));

        Assert.True(HubSurfaces.LinkTile.Image.TryParse(ValueOf(saved, HubSurfaces.LinkTile.Image.Key), out MediaRef? stored));
        Assert.Equal((photo.Url, 800), (stored!.Url, stored.Width));
        Assert.Equal([photo.Id], Media.UsedBy($"config.placement/{saved.Id}"));

        PatchMessage forged = LinkTile("Forged");
        forged.Values.Add(Value(HubSurfaces.LinkTile.Image, new MediaRef { Id = "missing", ContentType = "image/jpeg" }));
        PatchMessage privateImage = LinkTile("Private");
        privateImage.Values.Add(Value(HubSurfaces.LinkTile.Image, new MediaRef { Id = notes.Id, Key = notes.Key, ContentType = "image/jpeg" }));
        Draft draft = await NewDraftAsync(staff);

        Assert.Equal(StatusCode.InvalidArgument, (await Refused(() => SaveAsync(staff, draft, forged))).StatusCode);
        Assert.Contains("public image", (await Refused(() => SaveAsync(staff, draft, privateImage))).Status.Detail);
    }

    [Fact]
    public async Task FormattedTextWithAnUnsafeLinkIsNeverStored()
    {
        ClaimsPrincipal leader = Access.Person((EditModules.Node, "42"));
        Draft draft = await NewDraftAsync(leader);
        PatchMessage welcome = Add(Welcome, Modules, "42", (About.Key, ContentJson.Write(new RichText { Markdown = "[Click me](javascript:alert(1))" })));

        RpcException refused = await Refused(() => SaveAsync(leader, draft, welcome));

        Assert.Equal(StatusCode.InvalidArgument, refused.StatusCode);
    }

    private static ValueChange Value(MediaSetting setting, MediaRef media) => new() { Key = setting.Key, Value = setting.Format(media) };
}
