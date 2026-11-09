using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Client.Scopes;

public interface IScopeNames
{
    IReadOnlyCollection<string> Feeds { get; }

    Task<IReadOnlyDictionary<string, string>> NamesAsync(string scopeKind, IReadOnlyCollection<string> objectIds);

    Task<IReadOnlyList<ScopeChoice>?> ChoicesAsync(string scopeKind);

    Task<IReadOnlyDictionary<string, ScopeChoice>> PlacesAsync(string scopeKind, IReadOnlyCollection<string> objectIds);
}

public sealed record ScopeChoice(string Id, string Name, string? Href = null);

public interface IScopeNamer
{
    ScopeKind Kind { get; }

    IReadOnlyCollection<string> Feeds { get; }

    Task<IReadOnlyDictionary<string, string>> NamesAsync(IReadOnlyCollection<string> objectIds);

    Task<IReadOnlyList<ScopeChoice>> ChoicesAsync();
}

internal sealed class ScopeNames(IEnumerable<IScopeNamer> namers) : IScopeNames
{
    private readonly IReadOnlyList<IScopeNamer> _namers = namers.ToList();

    public IReadOnlyCollection<string> Feeds => _namers.SelectMany(namer => namer.Feeds).Distinct().ToList();

    public async Task<IReadOnlyDictionary<string, string>> NamesAsync(string scopeKind, IReadOnlyCollection<string> objectIds)
    {
        Dictionary<string, string> names = objectIds.Distinct().ToDictionary(id => id, id => id);
        if (_namers.FirstOrDefault(candidate => candidate.Kind.Name == scopeKind) is { } namer && names.Count > 0)
            foreach ((string id, string name) in await namer.NamesAsync(names.Keys.ToList()))
                if (names.ContainsKey(id))
                    names[id] = name;
        return names;
    }

    public async Task<IReadOnlyDictionary<string, ScopeChoice>> PlacesAsync(string scopeKind, IReadOnlyCollection<string> objectIds)
    {
        IReadOnlyList<ScopeChoice> known = objectIds.Count > 0 && await ChoicesAsync(scopeKind) is { } choices ? choices : [];
        return objectIds.Distinct().ToDictionary(id => id, id => known.FirstOrDefault(choice => choice.Id == id) ?? new ScopeChoice(id, id));
    }

    public async Task<IReadOnlyList<ScopeChoice>?> ChoicesAsync(string scopeKind) =>
        _namers.FirstOrDefault(candidate => candidate.Kind.Name == scopeKind) is { } namer ? await namer.ChoicesAsync() : null;
}
