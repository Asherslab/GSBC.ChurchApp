using System.Security.Claims;
using System.Text.Json;
using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Configuration.Api.Tests;

[Integration("A draft saved before Home was fixed as the first tab is read from Postgres and published")]
public sealed class TabBarDraftTests : ConfigurationHost
{
    [Fact]
    public async Task HomeCantBeTakenOffTheBarButADraftThatDidSoStillPublishesWithHomeFirst()
    {
        ClaimsPrincipal editor = Access.Person((TabBarSurfaces.Edit.Node, null), (TabBarSurfaces.Publish.Node, null));
        Draft draft = await NewDraftAsync(editor, "No Home");
        PatchMessage hideHome = new()
        {
            PlacementId = Guid.CreateVersion7().ToString(), Area = TabBarSurfaces.Area.Key, Surface = TabBarSurfaces.Home.Key, Kind = (int)PatchKind.Add, Hidden = true,
        };

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => SaveAsync(editor, draft, hideHome));
        Assert.Contains(PlacementRules.HomeIsFixed, refused.Status.Detail);

        await WithDbAsync(async db =>
        {
            db.DraftChanges.Add(new DraftChangeRecord
            {
                Id = Guid.CreateVersion7(), DraftId = Guid.Parse(draft.Id), Area = hideHome.Area, PlacementId = Guid.Parse(hideHome.PlacementId),
                Surface = hideHome.Surface, Kind = PatchKind.Add, Patch = JsonSerializer.Serialize(hideHome, JsonSerializerOptions.Default),
            });
            return await db.SaveChangesAsync(Cancel);
        });
        await PublishAsync(editor, draft);

        List<StoredPlacement> live = (await LiveAsync(editor, TabBarSurfaces.Area))
            .Select(placement => new StoredPlacement(placement.Id, placement.Surface, placement.Order, placement.Hidden, placement.Audience, placement.StartsAt,
                placement.EndsAt, placement.Values.ToDictionary(value => value.Key, value => value.Value), 1))
            .ToList();
        Assert.Equal(["Home", "News", "Sermons"], TabBar.For(SurfaceLayout.ShownTo(TabBarSurfaces.All.Surfaces, live, Viewer.Anonymous, Clock.GetUtcNow().UtcDateTime)).Select(tab => tab.Word));
    }
}
