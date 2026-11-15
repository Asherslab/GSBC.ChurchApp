using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Map;

public interface IOutreachMapLayer
{
    OutreachMapJob Job { get; }

    IReadOnlyList<LayerCount> Shows { get; }

    IReadOnlyList<MapLine> Lines { get; }

    IReadOnlyList<MapArea> Areas { get; }

    IReadOnlyList<MapMarker> Markers { get; }

    Task<bool> TapAsync(MapPoint point, double reachMetres);

    void LetGo();
}

public sealed record LayerCount(OutreachLayer Layer, int Count);
