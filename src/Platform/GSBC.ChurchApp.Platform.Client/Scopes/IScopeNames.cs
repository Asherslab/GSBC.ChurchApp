namespace GSBC.ChurchApp.Platform.Client.Scopes;

public interface IScopeNames
{
    Task<IReadOnlyDictionary<string, string>> NamesAsync(string scopeKind, IReadOnlyCollection<string> objectIds);
}

internal sealed class UnnamedScopes : IScopeNames
{
    public Task<IReadOnlyDictionary<string, string>> NamesAsync(string scopeKind, IReadOnlyCollection<string> objectIds) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(objectIds.Distinct().ToDictionary(id => id, id => id));
}
