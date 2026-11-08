using Grpc.Core;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Media;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Permissions;
using static GSBC.ChurchApp.Features.Configuration.Api.PlacementEditor;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class PlacementRules(SurfaceCatalog surfaces, PermissionCatalog permissions, IMediaReferences media, DestinationCatalog destinations)
{
    public (SurfaceArea Area, string? ScopeId, Permission Needed) Required(string areaKey, string? requestedScope, bool publishing)
    {
        if (!surfaces.Areas.TryGetValue(areaKey, out SurfaceArea? area))
            throw new RpcException(new Status(StatusCode.NotFound, $"There's no area '{areaKey}' to arrange."));

        string? scopeId = string.IsNullOrWhiteSpace(requestedScope) ? null : requestedScope.Trim();
        if (area.ScopeKind == ScopeKind.Global && scopeId is not null)
            throw Invalid($"{area.Title} is arranged once for everyone, so it doesn't take a scope.");
        if (area.ScopeKind != ScopeKind.Global && scopeId is null)
            throw Invalid($"Choose which {area.ScopeKind.Name} to arrange.");

        return (area, scopeId, publishing ? area.PublishPermission : area.EditPermission);
    }

    public Surface SurfaceIn(SurfaceArea area, string key) =>
        surfaces.ByKey.TryGetValue(key, out Surface? surface) && surface.Area.Key == area.Key
            ? surface
            : throw Invalid($"'{key}' can't be placed in {area.Title}.");

    public static void RefuseSecondCopy(Surface surface, IEnumerable<string> placedSurfaces)
    {
        if (!surface.Repeatable && placedSurfaces.Contains(surface.Key))
            throw Conflict(surface.Title);
    }

    public async Task<PlacementState> ValidatedAsync(Surface surface, PlacementState state, CancellationToken cancellationToken)
    {
        PlacementState claimed = Validated(surface, state);
        return MediaHolders(surface).Count == 0 ? claimed : Validated(surface, await WithCurrentMediaAsync(surface, claimed, cancellationToken));
    }

    public async Task<PlacementState> WithCurrentMediaAsync(Surface surface, PlacementState state, CancellationToken cancellationToken)
    {
        List<(Setting Setting, IHoldsMedia Holder)> holders = MediaHolders(surface);
        List<MediaRef> refs = holders.SelectMany(field => field.Holder.MediaIn(state.Values.GetValueOrDefault(field.Setting.Key, ""))).ToList();
        Dictionary<string, MediaRef> verified = (await media.VerifyAsync(refs, cancellationToken)).DistinctBy(item => item.Id).ToDictionary(item => item.Id);
        SortedDictionary<string, string> values = new(state.Values);
        foreach ((Setting setting, IHoldsMedia holder) in holders)
        {
            if (values.TryGetValue(setting.Key, out string? text))
                values[setting.Key] = holder.WithMedia(text, verified);
        }
        return state with { Values = values };
    }

    public IReadOnlyList<MediaRef> MediaOf(string surfaceKey, PlacementState? state) =>
        state is null || !surfaces.ByKey.TryGetValue(surfaceKey, out Surface? surface)
            ? []
            : MediaHolders(surface).SelectMany(field => field.Holder.MediaIn(state.Values.GetValueOrDefault(field.Setting.Key, ""))).ToList();

    public List<MediaRef> MediaOfDraft(IEnumerable<PlacementPatch> patches) =>
        patches
            .Where(patch => patch.Kind != PatchKind.Remove)
            .SelectMany(patch => MediaOf(patch.Surface, PlacementState.Of(patch.Created())))
            .DistinctBy(item => item.Id)
            .ToList();

    public bool HoldsMedia(string surfaceKey) =>
        surfaces.ByKey.TryGetValue(surfaceKey, out Surface? surface) && MediaHolders(surface).Count > 0;

    private static List<(Setting Setting, IHoldsMedia Holder)> MediaHolders(Surface surface) =>
        surface.Settings.OfType<IHoldsMedia>().Select(holder => ((Setting)holder, holder)).ToList();

    private PlacementState Validated(Surface surface, PlacementState state)
    {
        state = Normalised(surface, state);
        List<string> problems = [.. surface.ProblemsWith(state.Values)];
        if (!Audience.TryParse(state.Audience, out Audience? audience))
            problems.Add($"'{state.Audience}' isn't an audience.");
        else if (audience is { Node: { } node } && !permissions.ByNode.ContainsKey(node))
            problems.Add($"No permission '{node}' exists to choose an audience by.");
        if (state is { StartsAt: { } starts, EndsAt: { } ends } && ends <= starts)
            problems.Add($"'{surface.Title}' has to end after it starts.");
        if (problems.Count == 0 && TabBarSurfaces.IsTab(surface) && TabProblem(OpensOf(surface, state)) is { } tab)
            problems.Add(tab);

        return problems.Count == 0 ? state with { Audience = audience!.Text } : throw Invalid(string.Join(" ", problems));
    }

    private static string OpensOf(Surface surface, PlacementState state) =>
        state.Values.GetValueOrDefault(TabSettings.Opens.Key) ?? surface.Settings.First(setting => setting.Key == TabSettings.Opens.Key).DefaultText;

    private string? TabProblem(string opens) => destinations.Resolve(opens) switch
    {
        null => "That tab opens a place the app doesn't have. Choose another place.",
        ResolvedItem { Kind.OpensOutside: true } item => $"A {item.Kind.Noun.ToLowerInvariant()} opens outside the app, so it can't be a tab.",
        _ => null,
    };

    private static PlacementState Normalised(Surface surface, PlacementState state)
    {
        SortedDictionary<string, string> values = new(state.Values);
        foreach (Setting setting in surface.Settings.Where(setting => setting is INormalises && values.ContainsKey(setting.Key)))
            values[setting.Key] = ((INormalises)setting).Normalise(values[setting.Key]);
        return state with { Values = values };
    }
}
