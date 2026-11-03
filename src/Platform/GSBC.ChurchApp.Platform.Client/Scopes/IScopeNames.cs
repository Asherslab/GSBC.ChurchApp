using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Client.Scopes;

public interface IScopeNames
{
    IReadOnlyCollection<string> Feeds { get; }

    Task<IReadOnlyDictionary<string, string>> NamesAsync(string scopeKind, IReadOnlyCollection<string> objectIds);
}

public interface IScopeNamer
{
    ScopeKind Kind { get; }

    IReadOnlyCollection<string> Feeds { get; }

    Task<IReadOnlyDictionary<string, string>> NamesAsync(IReadOnlyCollection<string> objectIds);
}

internal sealed class ScopeNames(IEnumerable<IScopeNamer> namers) : IScopeNames
{
    private readonly IReadOnlyList<IScopeNamer> _namers = namers.ToList();

    public IReadOnlyCollection<string> Feeds => _namers.SelectMany(namer => namer.Feeds).Distinct().ToList();

    public async Task<IReadOnlyDictionary<string, string>> NamesAsync(string scopeKind, IReadOnlyCollection<string> objectIds)
    {
        Dictionary<string, string> names = objectIds.Distinct().ToDictionary(id => id, id => id);
        if (_namers.FirstOrDefault(namer => namer.Kind.Name == scopeKind) is { } namer && names.Count > 0)
            foreach ((string id, string name) in await namer.NamesAsync(names.Keys.ToList()))
                if (names.ContainsKey(id))
                    names[id] = name;
        return names;
    }
}
