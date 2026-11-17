using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Map;

public sealed class OutreachMapContext(string scope, OutreachAtlas atlas, OutreachMapRemembered remembered)
{
    private const double MetresPerDegree = 111_320;
    private const double MapWidthPixels = 900;
    private const double TapPixels = 16;
    private const double MinimumSpan = 0.003;
    private const double CardRoom = 2.2;

    private readonly List<IOutreachMapLayer> _layers = [];
    private readonly HashSet<OutreachLayer> _shown = [.. remembered.Layers];

    public string Scope { get; } = scope;

    public string? MinistryScope => Scope.Length == 0 ? null : Scope;

    public OutreachAtlas Atlas { get; set; } = atlas;

    public OutreachMapJob Job { get; private set; } = remembered.Job;

    public bool Open { get; private set; } = remembered.Open;

    public MapFrame? View { get; private set; } = remembered.View;

    public MapFrame? Frame { get; private set; } = remembered.View ?? atlas.Opening;

    public string? Campaign { get; private set; } = remembered.Campaign;

    public OutreachMapDrawing? Drawing { get; private set; }

    public int Placed { get; private set; }

    public IOutreachMapLayer? Chosen { get; private set; }

    public int Version { get; private set; }

    public IReadOnlyCollection<IOutreachMapLayer> Layers => _layers;

    public event Action? Changed;

    public event Action? Moved;

    public OutreachMapRemembered Remembered => new(Job, Open, [.. _shown.Order()], View, Campaign);

    public bool Shows(OutreachLayer layer) => _shown.Contains(layer);

    public void Show(OutreachLayer layer, bool on)
    {
        if (on ? _shown.Add(layer) : _shown.Remove(layer))
            Redraw();
    }

    public void Add(IOutreachMapLayer layer)
    {
        _layers.Add(layer);
        Redraw();
    }

    public void Remove(IOutreachMapLayer layer)
    {
        _layers.Remove(layer);
        if (ReferenceEquals(Chosen, layer))
            Chosen = null;
        Redraw();
    }

    public void Go(OutreachMapJob job)
    {
        StopDrawing();
        if (Chosen is { } chosen && chosen.Job != job)
        {
            chosen.LetGo();
            Chosen = null;
        }
        (Job, Open) = (job, true);
        Redraw();
    }

    public void Fold(bool open)
    {
        Open = open;
        Redraw();
    }

    public void Use(string campaign)
    {
        Campaign = campaign;
        Redraw();
    }

    public void FlyTo(MapFrame frame)
    {
        Frame = frame;
        Redraw();
    }

    public void FlyBesideCard(IReadOnlyList<MapPoint> points)
    {
        MapFrame around = MapFrame.Around(points, padding: 60, maxZoom: 17);
        double across = Math.Max(around.East - around.West, MinimumSpan);
        double up = Math.Max(around.North - around.South, MinimumSpan);
        double middle = (around.South + around.North) / 2;
        double west = around.West - across / 4;
        FlyTo(new MapFrame(middle - up / 2, west, middle + up / 2, west + across * CardRoom, 60, 17));
    }

    public IReadOnlyList<MapFrame> Clearing { get; private set; } = [];

    public void Clear(IReadOnlyList<MapFrame> boxes)
    {
        Clearing = boxes;
        Redraw();
    }

    public void SawView(MapFrame view)
    {
        View = view;
        Moved?.Invoke();
    }

    public void Choose(IOutreachMapLayer layer)
    {
        foreach (IOutreachMapLayer other in _layers.Where(other => !ReferenceEquals(other, layer)))
            other.LetGo();
        Chosen = layer;
        Redraw();
    }

    public void LetGo(IOutreachMapLayer layer)
    {
        if (!ReferenceEquals(Chosen, layer))
            return;
        Chosen = null;
        Redraw();
    }

    public void Draw(OutreachMapDrawing drawing)
    {
        (Drawing, Placed) = (drawing, 0);
        Redraw();
    }

    public void StopDrawing()
    {
        if (Drawing is null)
            return;
        (Drawing, Placed) = (null, 0);
        Redraw();
    }

    public void PlacedPoints(int count)
    {
        Placed = count;
        Redraw();
    }

    public async Task DrawnAsync(IReadOnlyList<MapPoint> points)
    {
        if (Drawing is not { } drawing)
            return;
        (Drawing, Placed) = (null, 0);
        await drawing.Drawn(points);
        Redraw();
    }

    public async Task TapAsync(MapPoint point)
    {
        if (Drawing is not null)
            return;
        double reach = ReachMetres(point);
        foreach (IOutreachMapLayer layer in _layers.OrderBy(layer => layer.Job == Job ? 0 : 1).ThenBy(layer => TapOrder(layer.Job)))
            if (await layer.TapAsync(point, reach))
                return;
    }

    public void Redraw()
    {
        Version++;
        Changed?.Invoke();
    }

    private double ReachMetres(MapPoint point)
    {
        if ((View ?? Frame) is not { } view)
            return 40;
        double metresAcross = (view.East - view.West) * MetresPerDegree * Math.Cos(point.Lat * Math.PI / 180);
        return Math.Max(4, metresAcross / MapWidthPixels * TapPixels);
    }

    private static int TapOrder(OutreachMapJob job) => job switch
    {
        OutreachMapJob.Reports => 0,
        OutreachMapJob.Places => 1,
        OutreachMapJob.SavedAreas => 2,
        _ => 3,
    };
}
