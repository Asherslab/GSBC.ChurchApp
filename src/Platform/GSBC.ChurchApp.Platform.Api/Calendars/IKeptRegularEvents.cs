using System.Security.Claims;

namespace GSBC.ChurchApp.Platform.Api.Calendars;

public interface IKeptRegularEvents
{
    ValueTask<KeptCalendar?> WhereAsync(string keeper, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<KeptCalendar>> ChoicesAsync(ClaimsPrincipal user, CancellationToken cancellationToken);

    ValueTask PutInAsync(ClaimsPrincipal user, Guid calendarId, KeptRegularEvent wanted, CancellationToken cancellationToken);

    ValueTask KeepInStepAsync(KeptRegularEvent wanted, CancellationToken cancellationToken);

    ValueTask RemoveAsync(string keeper, CancellationToken cancellationToken);
}

public sealed record KeptCalendar(Guid Id, string Name);

public sealed record KeptRegularEvent(string Keeper, string KeptBy, string OpensAt, string Title, string About, IReadOnlyList<KeptDate> On, IReadOnlyList<KeptGap> NotOn);

public sealed record KeptDate(DateOnly Date, TimeOnly Starts, TimeOnly Ends);

public sealed record KeptGap(DateOnly Date, string Why);
