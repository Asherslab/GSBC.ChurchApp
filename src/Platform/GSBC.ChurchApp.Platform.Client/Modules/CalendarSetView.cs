using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record CalendarInSet(string Id, string From, IReadOnlySet<string>? Days = null);

public sealed record CalendarSetView(Func<IReadOnlyList<CalendarInSet>, RenderFragment> ComingUp);
