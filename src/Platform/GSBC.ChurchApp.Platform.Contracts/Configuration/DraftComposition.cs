namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public enum PatchKind
{
    Change,
    Add,
    Remove,
}

public sealed record PlacementPatch(
    string PlacementId,
    string Area,
    string? ScopeId,
    string Surface,
    PatchKind Kind,
    int? Order = null,
    bool? Hidden = null,
    string? Audience = null,
    bool SetsDates = false,
    DateTime? StartsAt = null,
    DateTime? EndsAt = null,
    IReadOnlyDictionary<string, string?>? Values = null,
    StoredPlacement? Base = null)
{
    public IReadOnlyDictionary<string, string?> ValueChanges => Values ?? NoValues;

    public bool Touches(string area, string? scopeId) => Area == area && ScopeId == scopeId;

    public IEnumerable<string> Fields
    {
        get
        {
            if (Order is not null)
                yield return PatchField.Order;
            if (Hidden is not null)
                yield return PatchField.Hidden;
            if (Audience is not null)
                yield return PatchField.Audience;
            if (SetsDates)
                yield return PatchField.Dates;
            foreach (string key in ValueChanges.Keys)
                yield return PatchField.Value(key);
        }
    }

    public StoredPlacement ApplyTo(StoredPlacement placement)
    {
        Dictionary<string, string> values = new(placement.Values);
        foreach ((string key, string? value) in ValueChanges)
        {
            if (value is null)
                values.Remove(key);
            else
                values[key] = value;
        }

        return placement with
        {
            Order = Order ?? placement.Order,
            Hidden = Hidden ?? placement.Hidden,
            Audience = Audience ?? placement.Audience,
            StartsAt = SetsDates ? StartsAt : placement.StartsAt,
            EndsAt = SetsDates ? EndsAt : placement.EndsAt,
            Values = values,
        };
    }

    public StoredPlacement Created() =>
        ApplyTo(new StoredPlacement(PlacementId, Surface, 0, false, "", null, null, new Dictionary<string, string>(), 0));

    private static readonly IReadOnlyDictionary<string, string?> NoValues = new Dictionary<string, string?>();
}

public static class PatchField
{
    public const string Order = "order";
    public const string Hidden = "hidden";
    public const string Audience = "audience";
    public const string Dates = "dates";
    private const string ValuePrefix = "value:";

    public static string Value(string key) => ValuePrefix + key;

    public static bool IsValue(string field, out string key)
    {
        key = field.StartsWith(ValuePrefix, StringComparison.Ordinal) ? field[ValuePrefix.Length..] : "";
        return key.Length > 0;
    }

    public static object? Read(StoredPlacement? placement, string field) => placement is null ? null : field switch
    {
        Order => placement.Order,
        Hidden => placement.Hidden,
        Audience => placement.Audience,
        Dates => (placement.StartsAt, placement.EndsAt),
        _ when IsValue(field, out string key) => placement.Values.GetValueOrDefault(key),
        _ => null,
    };
}

public sealed record EventLayer(string DraftId, string Name, DateTime? StartsAt, DateTime? EndsAt, IReadOnlyList<PlacementPatch> Patches)
{
    public bool IsOnAt(DateTime now) => (StartsAt is null || now >= StartsAt) && (EndsAt is null || now < EndsAt);
}

public sealed record ChangedForEveryone(PlacementPatch Patch, IReadOnlyList<string> Fields, bool Removed);

public static class DraftComposition
{
    public static IReadOnlyList<StoredPlacement> Compose(
        IEnumerable<StoredPlacement> live, EventLayer? layer, IEnumerable<PlacementPatch> draft, DateTime now)
    {
        IEnumerable<StoredPlacement> underneath = layer is not null && layer.IsOnAt(now) ? Apply(live, layer.Patches) : live;
        return Apply(underneath, draft);
    }

    public static IReadOnlyList<StoredPlacement> Apply(IEnumerable<StoredPlacement> placements, IEnumerable<PlacementPatch> patches)
    {
        List<StoredPlacement> result = [.. placements];
        foreach (PlacementPatch patch in patches)
        {
            int index = result.FindIndex(placement => placement.Id == patch.PlacementId);
            switch (patch.Kind)
            {
                case PatchKind.Remove when index >= 0:
                    result.RemoveAt(index);
                    break;
                case PatchKind.Change when index >= 0:
                    result[index] = patch.ApplyTo(result[index]);
                    break;
                case PatchKind.Add when index < 0:
                    result.Add(patch.Created());
                    break;
            }
        }
        return result;
    }

    public static IReadOnlyList<ChangedForEveryone> ChangedSince(IEnumerable<StoredPlacement> live, IEnumerable<PlacementPatch> draft)
    {
        Dictionary<string, StoredPlacement> byId = live.ToDictionary(placement => placement.Id);
        List<ChangedForEveryone> changed = [];
        foreach (PlacementPatch patch in draft.Where(patch => patch.Kind != PatchKind.Add))
        {
            if (!byId.TryGetValue(patch.PlacementId, out StoredPlacement? now))
            {
                if (patch.Base is not null)
                    changed.Add(new ChangedForEveryone(patch, [], Removed: true));
                continue;
            }
            if (patch.Base is null || patch.Base.Revision == now.Revision)
                continue;

            StoredPlacement wanted = patch.Kind == PatchKind.Remove ? patch.Base : patch.ApplyTo(now);
            List<string> fields = (patch.Kind == PatchKind.Remove ? EveryField(patch.Base, now) : patch.Fields)
                .Where(field => Differs(patch.Base, now, field) && Differs(now, wanted, field))
                .ToList();
            if (fields.Count > 0)
                changed.Add(new ChangedForEveryone(patch, fields, Removed: false));
        }
        return changed;
    }

    private static bool Differs(StoredPlacement left, StoredPlacement right, string field) =>
        !Equals(PatchField.Read(left, field), PatchField.Read(right, field));

    private static IEnumerable<string> EveryField(StoredPlacement before, StoredPlacement now) =>
        new[] { PatchField.Order, PatchField.Hidden, PatchField.Audience, PatchField.Dates }
            .Concat(before.Values.Keys.Union(now.Values.Keys).Select(PatchField.Value));
}
