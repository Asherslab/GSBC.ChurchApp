using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record CalendarInSet(string Id, string From);

public sealed record CalendarSetView(Func<IReadOnlyList<CalendarInSet>, RenderFragment> ComingUp);
