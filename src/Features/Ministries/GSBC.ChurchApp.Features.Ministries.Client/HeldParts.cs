using System.Collections.Concurrent;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class HeldParts
{
    private readonly ConcurrentDictionary<string, Parts> _stored = new();
    private readonly ConcurrentDictionary<string, List<Parts>> _held = new();

    public void Hold(PartsSave save)
    {
        if (!_stored.TryGetValue(save.MinistryId, out Parts? stored))
            return;
        _held.AddOrUpdate(save.MinistryId, _ => [stored, stored.After(save)], (_, steps) => [.. steps, steps[^1].After(save)]);
    }

    public void Forget(string ministryId) => _held.TryRemove(ministryId, out _);

    public MinistryDetails Over(MinistryDetails ministry)
    {
        Parts stored = Parts.Of(ministry);
        _stored[ministry.Id] = stored;
        if (!_held.TryGetValue(ministry.Id, out List<Parts>? steps))
            return ministry;
        Parts latest = steps[^1];
        if (stored.Same(latest) || !steps.Any(stored.Same))
        {
            _held.TryRemove(ministry.Id, out _);
            return ministry;
        }
        MinistryDetails shown = MinistriesContractsModel.Instance.DeepClone(ministry);
        (shown.PartsOff, shown.PartsFromBelow, shown.ChatStarters, shown.PartsOrder) = ([.. latest.Off], [.. latest.FromBelow], latest.ChatStarters, [.. latest.Order]);
        return shown;
    }

    private sealed record Parts(IReadOnlyList<string> Off, IReadOnlyList<string> FromBelow, string ChatStarters, IReadOnlyList<string> Order)
    {
        public static Parts Of(MinistryDetails ministry) =>
            new(MinistryParts.Valid(ministry.PartsOff), MinistryParts.ValidFromBelow(ministry.PartsFromBelow), ministry.ChatStarters, MinistryParts.Ordered(ministry.PartsOrder));

        public Parts After(PartsSave save) =>
            new(MinistryParts.Valid(save.PartsOff), MinistryParts.ValidFromBelow(save.PartsFromBelow),
                Contracts.ChatStarters.IsValid(save.ChatStarters) ? save.ChatStarters : ChatStarters,
                save.PartsOrder.Count > 0 ? MinistryParts.Ordered(save.PartsOrder) : Order);

        public bool Same(Parts other) =>
            Off.SequenceEqual(other.Off) && FromBelow.SequenceEqual(other.FromBelow) && ChatStarters == other.ChatStarters && Order.SequenceEqual(other.Order);
    }
}
