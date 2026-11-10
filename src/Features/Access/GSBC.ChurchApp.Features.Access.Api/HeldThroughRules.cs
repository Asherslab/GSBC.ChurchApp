using GSBC.ChurchApp.Features.Access.Api.Data;
using GSBC.ChurchApp.Platform.Api.Access;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Access.Api;

internal sealed class HeldThroughRules(IEnumerable<SystemRole> systemRoles)
{
    private readonly List<(string Held, RoleThrough Through)> _declared =
        systemRoles.SelectMany(role => (role.Through ?? []).Select(through => (role.Key, through))).ToList();

    public bool Any => _declared.Count > 0;

    public async Task<IReadOnlyList<HeldThroughRule>> ResolveAsync(AccessDbContext db, CancellationToken cancellationToken)
    {
        if (_declared.Count == 0)
            return [];
        HashSet<string> keys = _declared.SelectMany(each => new[] { each.Held, each.Through.PersonRoleKey, each.Through.GivesRoleKey }).ToHashSet();
        Dictionary<string, Guid> ids = await db.Roles.AsNoTracking()
            .Where(role => role.SystemKey != null && keys.Contains(role.SystemKey))
            .ToDictionaryAsync(role => role.SystemKey!, role => role.Id, cancellationToken);
        return _declared
            .Where(each => ids.ContainsKey(each.Held) && ids.ContainsKey(each.Through.PersonRoleKey) && ids.ContainsKey(each.Through.GivesRoleKey))
            .Select(each => new HeldThroughRule(ids[each.Through.PersonRoleKey], ids[each.Held], ids[each.Through.GivesRoleKey]))
            .ToList();
    }
}
