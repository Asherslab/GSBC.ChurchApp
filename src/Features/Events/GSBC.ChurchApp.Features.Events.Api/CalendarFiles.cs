using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class CalendarFiles(EventsDbContext db, CalendarLinkSecrets links, CalendarFollowing following, IAccessEvaluator access, TimeProvider time)
{
    public const string PublicName = "All public calendars";
    public const string MineName = "All my calendars";

    private static readonly CalendarFile NotFound = new();

    public async Task<CalendarFile> ReadAsync(CalendarFileRequest request, CancellationToken cancellationToken)
    {
        if (request.Secret.Length == 0)
            return await PublicAsync(request.Calendar, cancellationToken);
        if (await links.HolderAsync(request.Secret, cancellationToken) is not { } holder)
            return NotFound;

        Viewer viewer = await ViewerAsync(holder, cancellationToken);
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        List<CalendarRecord> visible = book.Calendars.Values.Where(calendar => CalendarAudiences.Of(calendar.Audience).Includes(viewer)).ToList();
        if (request.Calendar.Length > 0)
            return visible.SingleOrDefault(calendar => calendar.Slug == request.Calendar) is { } one
                ? File(book, one.Name, [one], viewer)
                : NotFound;

        MyCalendars mine = await following.MineAsync(holder.InstallId ?? Guid.Empty, holder.UserId, cancellationToken);
        return File(book, MineName, visible.Where(calendar => CalendarChoices.InYourWeek(EventsPublisher.CalendarPayload(calendar), mine)).ToList(), viewer);
    }

    private async Task<CalendarFile> PublicAsync(string slug, CancellationToken cancellationToken)
    {
        EventsBook book = await EventsBook.LoadAsync(db, cancellationToken);
        List<CalendarRecord> forEveryone = book.Calendars.Values.Where(calendar => CalendarAudiences.Of(calendar.Audience) == Audience.Everyone).ToList();
        if (slug == CalendarLinks.Public)
            return File(book, PublicName, forEveryone, Viewer.Anonymous);
        return forEveryone.SingleOrDefault(calendar => calendar.Slug == slug) is { } one ? File(book, one.Name, [one], Viewer.Anonymous) : NotFound;
    }

    private async Task<Viewer> ViewerAsync(LinkHolder holder, CancellationToken cancellationToken)
    {
        if (holder.UserId is not { } person)
            return Viewer.Anonymous;
        GrantSet grants = await access.GrantsForAsync(person, cancellationToken);
        return new Viewer(true, node => PermissionNode.IsValid(node) && grants.IsAllowed(new Permission(node, ScopeKind.Global, node)));
    }

    private CalendarFile File(EventsBook book, string name, IReadOnlyCollection<CalendarRecord> calendars, Viewer viewer)
    {
        DateTime now = time.GetUtcNow().UtcDateTime;
        HashSet<string> chosen = calendars.Select(calendar => calendar.Id.ToString()).ToHashSet();
        (DateOnly from, DateOnly to) = EventsBook.Window(time);
        IEnumerable<EventOccurrence> occurrences = book.Occurrences(from, to, item => EventsBook.IsShown(item, now))
            .Where(occurrence => !occurrence.NotOn
                && (chosen.Contains(occurrence.CalendarId) || occurrence.AlsoOn.Any(chosen.Contains))
                && book.AudienceOf(occurrence).Includes(viewer))
            .OrderBy(occurrence => occurrence.StartsAt);
        return new CalendarFile { Found = true, Text = ICalWriter.Write(name, occurrences, now) };
    }
}
