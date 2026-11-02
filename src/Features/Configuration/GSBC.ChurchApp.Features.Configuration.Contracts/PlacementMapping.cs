using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

public static class PlacementMapping
{
    public static StoredPlacement ToStored(this Placement placement) =>
        new(placement.Id, placement.Surface, placement.Order, placement.Hidden, placement.Audience, placement.StartsAt, placement.EndsAt,
            placement.Values.GroupBy(value => value.Key).ToDictionary(group => group.Key, group => group.Last().Value), placement.Revision);

    public static Placement ToMessage(this StoredPlacement placement, string area, string? scopeId) => new()
    {
        Id = placement.Id,
        Area = area,
        ScopeId = scopeId,
        Surface = placement.Surface,
        Order = placement.Order,
        Hidden = placement.Hidden,
        Audience = placement.Audience,
        StartsAt = placement.StartsAt,
        EndsAt = placement.EndsAt,
        Values = placement.Values.Select(pair => new SettingValue { Key = pair.Key, Value = pair.Value }).ToList(),
        Revision = placement.Revision,
    };

    public static PlacementPatch ToPatch(this PatchMessage message) =>
        new(message.PlacementId, message.Area, message.ScopeId, message.Surface, (PatchKind)message.Kind,
            message.Order, message.Hidden, message.Audience, message.SetsDates, message.StartsAt, message.EndsAt,
            message.Values.GroupBy(change => change.Key).ToDictionary(group => group.Key, group => group.Last().Value),
            message.Base?.ToStored());

    public static PatchMessage ToMessage(this PlacementPatch patch) => new()
    {
        PlacementId = patch.PlacementId,
        Area = patch.Area,
        ScopeId = patch.ScopeId,
        Surface = patch.Surface,
        Kind = (int)patch.Kind,
        Order = patch.Order,
        Hidden = patch.Hidden,
        Audience = patch.Audience,
        SetsDates = patch.SetsDates,
        StartsAt = patch.StartsAt,
        EndsAt = patch.EndsAt,
        Values = patch.ValueChanges.Select(pair => new ValueChange { Key = pair.Key, Value = pair.Value }).ToList(),
        Base = patch.Base?.ToMessage(patch.Area, patch.ScopeId),
    };

    public static IReadOnlyList<PlacementPatch> Patches(this Draft draft) => draft.Changes.Select(ToPatch).ToList();

    public static EventLayer ToLayer(this EventLayerMessage layer) =>
        new(layer.DraftId, layer.Name, layer.StartsAt, layer.EndsAt, layer.Changes.Select(ToPatch).ToList());
}
