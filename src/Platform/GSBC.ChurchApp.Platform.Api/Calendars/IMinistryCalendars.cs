using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Api.Calendars;

public interface IMinistryCalendars
{
    ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(Guid ministryId, CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken);

    ValueTask ShowAsync(Guid ministryId, bool shown, Audience whileHidden, CancellationToken cancellationToken);

    ValueTask<MinistryCalendar> CreateAsync(Guid ministryId, string name, Audience audience, CancellationToken cancellationToken);

    ValueTask<MinistryCalendar> ChangeAsync(Guid ministryId, Guid calendarId, string name, Audience audience, CancellationToken cancellationToken);

    ValueTask<MinistryCalendarRemoval> WhatGoesAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken);

    ValueTask RemoveAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken);
}

public sealed record MinistryCalendar(Guid Id, Guid MinistryId, string Name, string Slug, Audience Audience, bool Hidden);

public sealed record MinistryCalendarRemoval(string Name, int Events, int RegularEvents);
