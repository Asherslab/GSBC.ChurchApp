using GSBC.ChurchApp.Features.Prayer.Api.Data;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Api.Notifications;

namespace GSBC.ChurchApp.Features.Prayer.Api;

internal sealed class PrayerTelling(INotifications notifications)
{
    public static string NewSubject(Guid id) => $"prayer/{id:N}";

    public static string WallSubject(Guid id) => $"prayer/{id:N}/wall";


    public async Task NewRequestAsync(RequestRecord request, CancellationToken cancellationToken)
    {
        Notification arrived = new(PrayerNotifications.NewRequest, NotificationAudience.Holding(PrayerPermissions.Read),
            "A new prayer request", "Read it under Prayer, in the app or the admin panel.", PrayerDestinations.Request(request.Id.ToString()), NewSubject(request.Id),
            Action: "Read it") { Actor = request.UserId };
        if (request.Wall != WallState.Waiting)
        {
            await notifications.RaiseAsync(arrived, cancellationToken);
            return;
        }

        Notification forTheWall = new(PrayerNotifications.WaitingForWall, NotificationAudience.Holding(PrayerPermissions.Wall),
            "A prayer request asks for the prayer wall", "Read it under Prayer → To approve, and choose what the wall shows.",
            PrayerDestinations.Approve, WallSubject(request.Id), Action: "Read it") { Actor = request.UserId };
        await notifications.RaiseOneOfAsync([arrived, forTheWall], cancellationToken);
    }

    public Task PrayedForAsync(RequestRecord request, CancellationToken cancellationToken) =>
        request.UserId is { } owner
            ? notifications.RaiseAsync(new Notification(PrayerNotifications.PrayedFor, NotificationAudience.Person(owner),
                "Someone prayed for your request",
                request.Prayed == 1 ? "The first prayer for it on the prayer wall." : $"{request.Prayed} prayers for it on the prayer wall so far.",
                PrayerDestinations.Prayer.Path, $"prayer/{request.Id:N}/prayed/{request.Prayed}") { Actor = null }, cancellationToken)
            : Task.CompletedTask;

    public Task AnsweredAsync(RequestRecord request, CancellationToken cancellationToken) =>
        request.UserId is { } owner
            ? notifications.RaiseAsync(new Notification(PrayerNotifications.Answered, NotificationAudience.Person(owner),
                "Your prayer request is marked answered", "The pastoral team marked it answered. Praise God with us.",
                PrayerDestinations.Prayer.Path, $"prayer/{request.Id:N}/answered") { Actor = null }, cancellationToken)
            : Task.CompletedTask;

    public Task CaredForAsync(RequestRecord request, CancellationToken cancellationToken) =>
        notifications.SettleAsync(NewSubject(request.Id), cancellationToken);

    public Task WallDecidedAsync(RequestRecord request, CancellationToken cancellationToken) =>
        notifications.SettleAsync(WallSubject(request.Id), cancellationToken);

    public async Task SettledAsync(RequestRecord request, CancellationToken cancellationToken)
    {
        await CaredForAsync(request, cancellationToken);
        await WallDecidedAsync(request, cancellationToken);
    }
}
