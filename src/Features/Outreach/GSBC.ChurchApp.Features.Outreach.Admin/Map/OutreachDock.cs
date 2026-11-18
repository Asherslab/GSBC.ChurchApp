using GSBC.ChurchApp.Platform.Ui.Maps;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Map;

public sealed record OutreachDock(IReadOnlyList<MapWorkspaceJob> Jobs, OutreachMapJob Current, Func<string, Task> Choose);
