using GSBC.ChurchApp.Platform.Client.People;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed class WideningWords(IServiceProvider services)
{
    private IReadOnlyList<RoleSummary>? _roles;

    public async Task<string?> SentenceAsync(bool widens, string newlyReaches, IEnumerable<string> roleIds, string onlyNow)
    {
        if (!widens)
            return null;
        string who = newlyReaches.Length > 0 ? newlyReaches : await RolesAsync(roleIds);
        return $"This will show to {who}, not only {onlyNow}.";
    }

    private async Task<string> RolesAsync(IEnumerable<string> roleIds)
    {
        if (_roles is null && services.GetService(typeof(IRoleDirectory)) is IRoleDirectory directory)
            _roles = await directory.ListAsync();
        List<string> names = roleIds.Select(id => _roles?.FirstOrDefault(role => role.Id.ToString() == id)?.Name).OfType<string>().ToList();
        return names.Count switch
        {
            0 => "people in other roles",
            1 => $"people with the {names[0]} role",
            _ => $"people with the {string.Join(", ", names[..^1])} or {names[^1]} roles",
        };
    }
}
