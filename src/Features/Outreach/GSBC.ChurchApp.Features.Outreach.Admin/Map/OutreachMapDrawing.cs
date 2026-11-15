using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Map;

public sealed record OutreachMapDrawing(MapSketchTool Tool, string Text, Func<IReadOnlyList<MapPoint>, Task> Drawn, string FinishLabel = "Finish");
