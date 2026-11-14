using GSBC.ChurchApp.Platform.Api.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Api.Content;

public interface IAppCardSource
{
    IReadOnlyList<AppCardKind> Kinds { get; }

    Task<FoundPlace?> FindAsync(AppCard card, CancellationToken cancellationToken);
}
