using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryDestinationSource(MinistryViews views) : IDestinationSource
{
    public DestinationKind Kind => MinistriesDestinations.Ministry;

    public async Task<IReadOnlyList<FoundPlace>> SearchAsync(string text, int take, CancellationToken cancellationToken)
    {
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        string wanted = text.Trim();
        return tree.All.Where(ministry => wanted.Length == 0 || ministry.Name.Contains(wanted, StringComparison.CurrentCultureIgnoreCase))
            .OrderBy(ministry => ministry.Name, StringComparer.CurrentCulture).Take(take)
            .Select(ministry => Place(tree, ministry))
            .ToList();
    }

    public async Task<FoundPlace?> FindAsync(string id, CancellationToken cancellationToken)
    {
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        return Guid.TryParse(id, out Guid ministryId) && tree.Find(ministryId) is { } found ? Place(tree, found) : null;
    }

    public Task<string?> OpenUrlAsync(string id, CancellationToken cancellationToken) => Task.FromResult<string?>(null);

    internal static FoundPlace Place(MinistryTree tree, MinistryRecord ministry) =>
        tree.IsHidden(ministry.Id)
            ? new(ministry.Id.ToString(), ministry.Name, "Ministries", MinistryViews.SeesHidden(ministry.Id), ministry.Id.ToString())
            : new(ministry.Id.ToString(), ministry.Name, "Ministries", Audience.Everyone);
}
