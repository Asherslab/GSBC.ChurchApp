namespace Gsbc.AppPoc.Data.Outreach;

public enum MapTool { Select, Scissors, Brush, Lasso, Eraser, Start }

public sealed record RoutePick(int Route, bool Add);

public sealed record MapStroke(MapTool Tool, IReadOnlyList<int> Edges);

public sealed record StartMove(int Route, GeoPoint Point);
