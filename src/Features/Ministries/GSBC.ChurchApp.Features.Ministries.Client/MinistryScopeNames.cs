using Grpc.Core;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Scopes;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Ministries.Client;

internal sealed class MinistryScopeNames(MinistriesClient ministries) : IScopeNamer
{
    public ScopeKind Kind => ScopeKind.Ministry;

    public IReadOnlyCollection<string> Feeds { get; } = [MinistriesFeeds.Ministries];

    public async Task<IReadOnlyList<ScopeChoice>> ChoicesAsync()
    {
        try
        {
            return (await ministries.DirectoryAsync()).Ministries
                .Select(ministry => new ScopeChoice(ministry.Id, ministry.Name, $"ministries/{ministry.Id}"))
                .ToList();
        }
        catch (RpcException)
        {
            return [];
        }
    }
}
