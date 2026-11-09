using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public sealed class DatedChanges(ConfigurationClient configuration, SurfaceCatalog surfaces, IAccess access)
{
    private const string EventLayer = "Event layer";

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

    public async Task<IReadOnlyList<ComingUp>> ComingUpAsync(DraftList list, DateTime from, DateTime until)
    {
        List<ComingUp> coming = [];
        foreach (Draft layer in list.Drafts.Where(draft => draft.LayerOn && (draft.LayerStartsAt ?? from) < until && (draft.LayerEndsAt ?? until) > from))
        {
            if (layer.LayerStartsAt is { } starts && starts > from)
                coming.Add(new ComingUp(starts, $"“{layer.Name}” goes on for everyone{Until(layer.LayerEndsAt)}", layer.Id));
            else if (layer.LayerEndsAt is { } ends)
                coming.Add(new ComingUp(ends, $"“{layer.Name}” goes off, and everyone is back to what's published", layer.Id));
        }

        foreach (Draft scheduled in list.Drafts.Where(draft => draft is { PublishAt: { } at, NotPublished: null } && at > from && at < until))
            coming.Add(new ComingUp(scheduled.PublishAt!.Value, $"“{scheduled.Name}” is published for good", scheduled.Id));

        foreach (SurfaceArea area in surfaces.Areas.Values.Where(area => area.ScopeKind == ScopeKind.Global && access.Can(area.EditPermission)))
        {
            AreaPlacements live = await configuration.GetAreaAsync(area, null);
            IReadOnlyList<ArrangedSurface> arranged = SurfaceLayout.Arrange(surfaces.In(area), live.Placements.Select(placement => placement.ToStored()));
            foreach (ArrangedSurface entry in arranged.Where(entry => !entry.Hidden))
            {
                if (entry.StartsAt is { } starts && starts > from && starts < until)
                    coming.Add(new ComingUp(starts, Starts(entry, arranged), null));
                if (entry.EndsAt is { } ends && ends > from && ends < until)
                    coming.Add(new ComingUp(ends, Ends(entry), null));
            }
        }

        return coming.OrderBy(item => item.At).ToList();
    }

    private static string Starts(ArrangedSurface entry, IReadOnlyList<ArrangedSurface> area) =>
        entry.Surface == AppearanceSurfaces.Theme.Surface ? $"The app's colours change to {ThemeNames.Of(entry.Values.Get(AppearanceSurfaces.Theme.Palette), area)}"
        : TabBarSurfaces.IsTab(entry.Surface) ? $"The “{DraftWords.NameOf(entry)}” tab goes on the tab bar"
        : $"“{DraftWords.NameOf(entry)}” appears on Home";

    private static string Ends(ArrangedSurface entry) =>
        entry.Surface == AppearanceSurfaces.Theme.Surface ? "The app's colours go back to the church's brand"
        : TabBarSurfaces.IsTab(entry.Surface) ? $"The “{DraftWords.NameOf(entry)}” tab comes off the tab bar"
        : $"“{DraftWords.NameOf(entry)}” leaves Home";

    private static string Until(DateTime? ends) => ends is { } at ? $", until {DraftWords.Day(at)}" : "";

    private static bool Overlaps(DatedSpan span, DateTime from, DateTime until) => span.EndsAt > from && span.StartsAt < until;

    private string TitleOf(PlacementPatch patch) =>
        patch.ValueChanges.GetValueOrDefault(HubSurfaces.LinkTile.Title.Key) ?? surfaces.ByKey.GetValueOrDefault(patch.Surface)?.Title ?? patch.Surface;
}

public sealed record ComingUp(DateTime At, string Words, string? DraftId);

public sealed record DatedLane(string Title, IReadOnlyList<DatedSpan> Spans);

public sealed record DatedSpan(string Title, DateTime StartsAt, DateTime EndsAt, DatedKind Kind, string? DraftId);

public enum DatedKind
{
    Everyone,
    Layer,
    Draft,
}
