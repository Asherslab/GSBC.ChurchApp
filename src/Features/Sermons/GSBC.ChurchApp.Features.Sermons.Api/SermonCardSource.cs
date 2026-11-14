using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Api.Content;
using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Features.Sermons.Api;

internal sealed class SermonCardSource(IEnumerable<IDestinationSource> destinations) : IAppCardSource
{
    public IReadOnlyList<AppCardKind> Kinds => [AppCardKind.Sermon];

    public Task<FoundPlace?> FindAsync(AppCard card, CancellationToken cancellationToken) =>
        destinations.Single(source => source.Kind == SermonsDestinations.Sermon).FindAsync(card.Id, cancellationToken);
}
