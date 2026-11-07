using System.Security.Claims;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

[Integration("Undo rebuilds an area from the change rows its publish saved, and records itself as a batch of its own")]
public sealed class UndoHistoryTests : ConfigurationHost
{
    private static readonly string Title = HubSurfaces.LinkTile.Title.Key;

    [Fact]
    public async Task UndoingAPublishedDraftRestoresWhatWasThereIsAuditedAndCanBePublishedAgain()
    {
        ClaimsPrincipal staff = Access.Person(ArrangesHub, PublishesHub);
        Placement bible = Assert.Single(await PublishNowAsync(staff, HubSurfaces.Area, null, LinkTile("Bible")));
        Clock.Advance(TimeSpan.FromMinutes(1));
        await PublishNowAsync(staff, HubSurfaces.Area, null, Change(bible, (Title, "Read the Bible")), LinkTile("Camp"));

        ChangeList history = await ChangesAsync(staff);
        Assert.Equal("A quick fix", history.Batches[0].DraftName);
        Clock.Advance(TimeSpan.FromMinutes(1));
        await Placements(editor => editor.RollBackAsync(staff, new RollBackRequest { BatchId = history.Batches[0].Id }, Cancel));

        Placement restored = Assert.Single(await LiveAsync(staff, HubSurfaces.Area));
        Assert.Equal((bible.Id, "Bible"), (restored.Id, ValueOf(restored, Title)));
        ChangeBatch undo = (await ChangesAsync(staff)).Batches[0];
        Assert.Equal(history.Batches[0].Id, undo.RollsBack);
        Assert.Equal(2, undo.Changes.Count);

        Clock.Advance(TimeSpan.FromMinutes(1));
        await Placements(editor => editor.RollBackAsync(staff, new RollBackRequest { BatchId = undo.Id }, Cancel));

        Assert.Equal(["Read the Bible", "Camp"], (await LiveAsync(staff, HubSurfaces.Area)).Select(tile => ValueOf(tile, Title)).Order().Reverse());
    }

    private Task<ChangeList> ChangesAsync(ClaimsPrincipal staff) =>
        Placements(editor => editor.ListChangesAsync(staff, new AreaQuery { Area = HubSurfaces.Area.Key }, Cancel));
}
