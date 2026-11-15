using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Map;

public abstract class OutreachMapLayer : OutreachMapPart, IOutreachMapLayer
{
    public abstract OutreachMapJob Job { get; }

    public virtual IReadOnlyList<LayerCount> Shows => [];

    public virtual IReadOnlyList<MapLine> Lines => [];

    public virtual IReadOnlyList<MapArea> Areas => [];

    public virtual IReadOnlyList<MapMarker> Markers => [];

    protected bool IsOpen => Map.Job == Job && Map.Open;

    protected bool HasCard => ReferenceEquals(Map.Chosen, this);

    protected string? MinistryScope => Map.MinistryScope;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Map.Add(this);
    }

    public virtual Task<bool> TapAsync(MapPoint point, double reachMetres) => Task.FromResult(false);

    public virtual void LetGo()
    {
    }

    public override void Dispose()
    {
        Map.Remove(this);
        base.Dispose();
    }

    protected static double MetresBetween(MapPoint one, MapPoint other)
    {
        double across = (other.Lon - one.Lon) * 111_320 * Math.Cos(one.Lat * Math.PI / 180);
        double up = (other.Lat - one.Lat) * 111_320;
        return Math.Sqrt(across * across + up * up);
    }
}
