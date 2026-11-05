using GSBC.ChurchApp.Platform.Api.Media;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed class PlacementMediaHolder(PlacementEditor placements) : IMediaHolder
{
    public bool Holds(string owner) => PlacementEditor.IsMediaOwner(owner, out _);

    public Task RefreshAsync(string owner, CancellationToken cancellationToken) =>
        PlacementEditor.IsMediaOwner(owner, out Guid placementId) ? placements.RefreshMediaAsync(placementId, cancellationToken) : Task.CompletedTask;
}
