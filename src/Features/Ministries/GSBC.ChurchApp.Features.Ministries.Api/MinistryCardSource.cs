using GSBC.ChurchApp.Platform.Api.Content;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed class MinistryCardSource(MinistryViews views) : IAppCardSource
{
    public IReadOnlyList<AppCardKind> Kinds => [AppCardKind.Ministry, AppCardKind.LifeGroup];

    public async Task<FoundPlace?> FindAsync(AppCard card, CancellationToken cancellationToken)
    {
        MinistryTree tree = await views.TreeAsync(cancellationToken);
        if (!Guid.TryParse(card.Id, out Guid id) || tree.Find(id) is not { ArchivedAt: null } ministry)
            return null;
        if (card.Kind == AppCardKind.LifeGroup && !ministry.ShowsInLifeGroups)
            return null;
        return MinistryDestinationSource.Place(tree, ministry);
    }
}
