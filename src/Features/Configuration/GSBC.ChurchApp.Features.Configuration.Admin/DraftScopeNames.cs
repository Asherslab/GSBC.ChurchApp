using Grpc.Core;
using GSBC.ChurchApp.Features.Configuration.Client;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Client.Scopes;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Configuration.Admin;

internal sealed class DraftScopeNames(DraftsClient drafts) : IScopeNamer
{
    public ScopeKind Kind => ScopeKind.Draft;

    public IReadOnlyCollection<string> Feeds { get; } = [ConfigurationFeeds.Drafts];

    public async Task<IReadOnlyDictionary<string, string>> NamesAsync(IReadOnlyCollection<string> objectIds) =>
        (await ChoicesAsync()).Where(draft => objectIds.Contains(draft.Id)).ToDictionary(draft => draft.Id, draft => draft.Name);

    public async Task<IReadOnlyList<ScopeChoice>> ChoicesAsync()
    {
        try
        {
            return (await drafts.ListAsync()).Drafts.Select(draft => new ScopeChoice(draft.Id, draft.Name)).ToList();
        }
        catch (RpcException)
        {
            return [];
        }
    }
}
