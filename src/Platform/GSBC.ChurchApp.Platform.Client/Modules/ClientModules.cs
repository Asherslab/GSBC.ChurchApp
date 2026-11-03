using System.Reflection;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed class ClientModules(Assembly hostAssembly)
{
    private readonly List<ClientModule> _modules = [];

    public IReadOnlyList<ClientModule> All => _modules;

    public IEnumerable<Assembly> AdditionalAssemblies =>
        _modules.Select(module => module.PageAssembly).Where(assembly => assembly != hostAssembly).Distinct();

    public IEnumerable<NavEntry> Navigation => _modules.SelectMany(module => module.Navigation);

    public IReadOnlyCollection<string> Feeds => _modules.SelectMany(module => module.Feeds ?? []).Concat(PrivateFeeds).ToHashSet();

    public IReadOnlyCollection<string> PrivateFeeds => _modules.SelectMany(module => module.PrivateFeeds ?? []).ToHashSet();

    public ClientModules Add(ClientModule module)
    {
        _modules.Add(module);
        return this;
    }
}
