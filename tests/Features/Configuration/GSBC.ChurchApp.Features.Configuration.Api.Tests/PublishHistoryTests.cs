using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

public sealed class PublishHistoryTests
{
    private static readonly DateTime Now = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Bible = Guid.CreateVersion7();
    private static readonly Guid Published = Guid.CreateVersion7();
    private static readonly Guid Undo = Guid.CreateVersion7();
    private static readonly PlacementState AsPublished = TileTitled("Read the Bible");
    private static readonly ChangeRecord[] Batch =
    [
        new() { BatchId = Published, Area = HubSurfaces.Area.Key, Surface = AsPublished.Surface, PlacementId = Bible, After = AsPublished.ToJson(), DraftName = "Hub Fixes" },
    ];

    [Fact]
    public void APublishShowsAsUndoneOnlyWhileItsChangesAreNotBack()
    {
        Rollback undo = new(Undo, Published, Now, Guid.CreateVersion7());

        Assert.Equal(Undo.ToString(), UndoneBatch(undos: [undo], redos: [], live: TileTitled("Bible")));
        Assert.Null(UndoneBatch(undos: [undo], redos: [], live: AsPublished));
        Assert.Null(UndoneBatch(undos: [undo], redos: [new Rollback(Guid.CreateVersion7(), Undo, Now.AddMinutes(1), undo.By)], live: TileTitled("Bible")));
    }

    [Fact]
    public void ARollbackThatChangesNothingSaysWhatTheAreaAlreadyLooksLike()
    {
        Assert.Equal("Nothing to change: the Home screen already looks as “Hub Fixes” left it.",
            PublishHistory.NothingToChange([HubSurfaces.Area.Title], draftName: null, undoneDraftName: "Hub Fixes"));
        Assert.Equal("Nothing to change: the Home screen and colours already look as they did before “Hub Fixes” was published.",
            PublishHistory.NothingToChange([HubSurfaces.Area.Title, "Colours"], draftName: "Hub Fixes", undoneDraftName: null));
    }

    private static string? UndoneBatch(Rollback[] undos, Rollback[] redos, PlacementState live)
    {
        PublishedDraft entry = new() { BatchId = Published.ToString(), Name = "Hub Fixes" };
        PublishHistory.MarkUndone([entry], Batch, undos, redos, new Dictionary<Guid, PlacementState> { [Bible] = live });
        return entry.UndoneBatchId;
    }

    private static PlacementState TileTitled(string title) =>
        new(HubSurfaces.LinkTile.Surface.Key, 0, false, "", null, null, new() { [HubSurfaces.LinkTile.Title.Key] = title });
}
