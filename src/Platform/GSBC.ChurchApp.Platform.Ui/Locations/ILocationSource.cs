using GSBC.ChurchApp.Platform.Contracts.Locations;
using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Platform.Ui.Locations;

public interface ILocationSource
{
    IReadOnlyCollection<string> Feeds { get; }

    Task<IReadOnlyList<Location>> LocationsAsync();
}

public interface IKeptMapCover
{
    Task<MapSource?> CoverAsync(double latitude, double longitude);
}
