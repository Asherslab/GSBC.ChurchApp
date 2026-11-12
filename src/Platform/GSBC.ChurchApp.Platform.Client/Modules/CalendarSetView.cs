using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record DayRange(DateOnly From, DateOnly To)
{
    public bool Holds(DateOnly day) => From <= day && day <= To;
}

public sealed record DayNote(DateOnly Day, string Title, string Href, string? NotOn = null);

public sealed record CalendarInSet(string Id, string From, IReadOnlyList<DayRange>? Within = null, IReadOnlyList<DayNote>? Notes = null);

public sealed record CalendarSetView(Func<IReadOnlyList<CalendarInSet>, RenderFragment> ComingUp);
