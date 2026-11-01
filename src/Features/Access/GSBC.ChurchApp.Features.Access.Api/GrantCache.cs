using System.Collections.Concurrent;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class GrantCache(TimeProvider time)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<Guid, (GrantSet Grants, DateTimeOffset Until)> _entries = new();

    public bool TryGet(Guid userId, out GrantSet grants)
    {
        if (_entries.TryGetValue(userId, out var entry) && entry.Until > time.GetUtcNow())
        {
            grants = entry.Grants;
            return true;
        }

        grants = GrantSet.Empty;
        return false;
    }

    public void Set(Guid userId, GrantSet grants) => _entries[userId] = (grants, time.GetUtcNow() + Lifetime);

    public void Clear() => _entries.Clear();
}
