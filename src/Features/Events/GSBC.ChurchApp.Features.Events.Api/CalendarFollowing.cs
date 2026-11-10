using System.Security.Claims;
using Grpc.Core;
using GSBC.ChurchApp.Features.Events.Api.Data;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Events.Api;

internal sealed class CalendarFollowing(EventsDbContext db, IRoleAssignments roles, IPhoneRoles phoneRoles, IAccessEvaluator access, TimeProvider time)
{
    public async Task<MyCalendars> ChooseAsync(Guid installId, ClaimsPrincipal user, CalendarChoice choice, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(choice.CalendarId, out Guid calendarId) || await db.Calendars.AsNoTracking().SingleOrDefaultAsync(found => found.Id == calendarId, cancellationToken) is not { } calendar)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "That calendar isn't here any more."));
        Guid? userId = user.Identity?.IsAuthenticated == true ? Caller.UserId(user) : null;
        if (choice.InYourWeek && WhyNotAdd(calendar, userId is not null, await ViewerAsync(access, userId, cancellationToken)) is { } refused)
            throw new RpcException(refused);

        bool shownAnyway = calendar.ShownOnNewPhones || CalendarAudiences.Of(calendar.Audience).IsForSomeone;
        string objectId = calendarId.ToString();
        (string give, string takeAway) = choice.InYourWeek ? (EventsPermissions.AdderRole, EventsPermissions.TurnedOffRole) : (EventsPermissions.TurnedOffRole, EventsPermissions.AdderRole);
        bool recordGiven = choice.InYourWeek ? !shownAnyway : shownAnyway;

        await TakeAwayAsync(installId, userId, takeAway, objectId, cancellationToken);
        if (recordGiven)
            await GiveAsync(installId, userId, give, objectId, cancellationToken);
        else
            await TakeAwayAsync(installId, userId, give, objectId, cancellationToken);
        if (userId is { } person)
            await PublishAsync([person], cancellationToken);
        return await MineAsync(installId, userId, cancellationToken);
    }

    public async Task PublishAsync(IReadOnlyCollection<Guid>? people, CancellationToken cancellationToken)
    {
        List<string> calendars = await db.Calendars.AsNoTracking().Select(calendar => calendar.Id.ToString()).ToListAsync(cancellationToken);
        List<Guid> everyone = people?.ToList() ?? [
            .. (await roles.HoldingsAsync(EventsPermissions.AdderRole, calendars, cancellationToken)).Select(holding => holding.UserId),
            .. (await roles.HoldingsAsync(EventsPermissions.TurnedOffRole, calendars, cancellationToken)).Select(holding => holding.UserId)];
        List<string> ids = everyone.Distinct().Select(person => person.ToString()).ToList();
        Dictionary<string, FeedChangeRecord> published = (await db.FeedChanges.AsNoTracking()
                .Where(change => change.Feed == EventsFeeds.Mine && (people == null || ids.Contains(change.RecordId)))
                .GroupBy(change => change.RecordId)
                .Select(versions => versions.OrderByDescending(change => change.Cursor).First())
                .ToListAsync(cancellationToken))
            .ToDictionary(change => change.RecordId);
        long version = time.GetUtcNow().UtcDateTime.Ticks;
        foreach (Guid person in everyone.Distinct())
        {
            using MemoryStream stream = new();
            EventsContractsModel.Instance.Serialize(stream, await MineAsync(Guid.Empty, person, cancellationToken));
            byte[] payload = stream.ToArray();
            if (published.GetValueOrDefault(person.ToString()) is { Deleted: false } before && before.Payload.AsSpan().SequenceEqual(payload))
                continue;
            db.FeedChanges.Add(new FeedChangeRecord { Feed = EventsFeeds.Mine, RecordId = person.ToString(), ScopeId = person.ToString(), Version = version, Payload = payload });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MyCalendars> MineAsync(Guid installId, Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } person)
        {
            IReadOnlyList<HeldRole> held = await phoneRoles.HeldAsync(installId, cancellationToken);
            return new MyCalendars
            {
                Added = Held(held, EventsPermissions.AdderRole),
                TurnedOff = Held(held, EventsPermissions.TurnedOffRole),
            };
        }

        MyCalendars mine = new();
        foreach (Guid id in await db.Calendars.AsNoTracking().OrderBy(calendar => calendar.Order).Select(calendar => calendar.Id).ToListAsync(cancellationToken))
        {
            string objectId = id.ToString();
            if ((await roles.HoldersAsync(EventsPermissions.AdderRole, objectId, cancellationToken)).Any(holder => holder.UserId == person))
                mine.Added.Add(objectId);
            if ((await roles.HoldersAsync(EventsPermissions.TurnedOffRole, objectId, cancellationToken)).Any(holder => holder.UserId == person))
                mine.TurnedOff.Add(objectId);
        }
        return mine;
    }

    public static Status? WhyNotAdd(CalendarRecord calendar, bool signedIn, Viewer viewer) =>
        CalendarAudiences.Of(calendar.Audience).Includes(viewer) ? null
        : signedIn ? new Status(StatusCode.PermissionDenied, $"{calendar.Name} isn't for you.")
        : new Status(StatusCode.Unauthenticated, $"Sign in to add {calendar.Name}.");

    public static async Task<Viewer> ViewerAsync(IAccessEvaluator access, Guid? userId, CancellationToken cancellationToken)
    {
        if (userId is not { } person)
            return Viewer.Anonymous;
        GrantSet grants = await access.GrantsForAsync(person, cancellationToken);
        return Viewer.Of(grants);
    }

    private static List<string> Held(IReadOnlyList<HeldRole> held, string role) =>
        held.Where(each => each.RoleKey == role && each.ObjectId is not null).Select(each => each.ObjectId!).ToList();

    private async Task GiveAsync(Guid installId, Guid? userId, string role, string objectId, CancellationToken cancellationToken)
    {
        if (userId is { } person)
            await roles.GiveAsFeatureAsync(person, role, objectId, null, cancellationToken);
        else
            await phoneRoles.GiveAsync(installId, role, objectId, null, cancellationToken);
    }

    private async Task TakeAwayAsync(Guid installId, Guid? userId, string role, string objectId, CancellationToken cancellationToken)
    {
        if (userId is { } person)
            await roles.TakeAwayAsFeatureAsync(person, role, objectId, cancellationToken);
        else
            await phoneRoles.TakeAwayAsync(installId, role, objectId, cancellationToken);
    }
}
