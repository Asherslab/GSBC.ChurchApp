using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public sealed class DatedChanges(ConfigurationClient configuration, SurfaceCatalog surfaces, IAccess access)
{
    public const string EventLayer = "Event layer";

    public async Task<IReadOnlyList<DatedLane>> LanesAsync(DraftList list, DateTime from, DateTime until)
    {
        List<DatedLane> lanes = [new(EventLayer, list.Drafts.Where(draft => draft.LayerOn)
            .Select(draft => new DatedSpan(draft.Name, draft.LayerStartsAt ?? from, draft.LayerEndsAt ?? until, DatedKind.Layer, draft.Id))
            .Where(span => Overlaps(span, from, until))
            .ToList())];

        foreach (SurfaceArea area in surfaces.Areas.Values.Where(area => area.ScopeKind == ScopeKind.Global && access.Can(area.EditPermission)))
        {
            AreaPlacements live = await configuration.GetAreaAsync(area, null);
            List<DatedSpan> spans = SurfaceLayout.Arrange(surfaces.In(area), live.Placements.Select(placement => placement.ToStored()))
                .Where(entry => entry.StartsAt is not null || entry.EndsAt is not null)
                .Select(entry => new DatedSpan(DraftWords.NameOf(entry), entry.StartsAt ?? from, entry.EndsAt ?? until, DatedKind.Everyone, null))
                .ToList();
            spans.AddRange(list.Drafts.SelectMany(draft => draft.Patches()
                .Where(patch => patch.Area == area.Key && patch.SetsDates && (patch.StartsAt is not null || patch.EndsAt is not null))
                .Select(patch => new DatedSpan($"{draft.Name}: {TitleOf(patch)}", patch.StartsAt ?? from, patch.EndsAt ?? until, DatedKind.Draft, draft.Id))));
            lanes.Add(new DatedLane(area.Title, spans.Where(span => Overlaps(span, from, until)).ToList()));
        }

        return lanes;
    }

    private static bool Overlaps(DatedSpan span, DateTime from, DateTime until) => span.EndsAt > from && span.StartsAt < until;

    private string TitleOf(PlacementPatch patch) =>
        patch.ValueChanges.GetValueOrDefault(HubSurfaces.LinkTile.Title.Key) ?? surfaces.ByKey.GetValueOrDefault(patch.Surface)?.Title ?? patch.Surface;
}

public sealed record DatedLane(string Title, IReadOnlyList<DatedSpan> Spans);

public sealed record DatedSpan(string Title, DateTime StartsAt, DateTime EndsAt, DatedKind Kind, string? DraftId);

public enum DatedKind
{
    Everyone,
    Layer,
    Draft,
}
