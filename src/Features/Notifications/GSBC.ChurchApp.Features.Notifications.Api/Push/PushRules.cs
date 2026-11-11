using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Features.Notifications.Api.Push;

internal static class PushRules
{
    public const string InboxLink = "/notifications";
    private static readonly TimeSpan GiveUpAfter = TimeSpan.FromHours(1);
    private static readonly TimeSpan LongestBackoff = TimeSpan.FromMinutes(16);

    public static PushMessage Message(List<Delivery> deliveries, int? badge, DestinationCatalog places)
    {
        Delivery first = deliveries[0];
        if (first.Kind == DeliveryKind.Background)
            return new PushMessage(null, null, null, null, null, null, null, Urgent: false, Background: true);

        NotificationRecord latest = deliveries.MaxBy(delivery => delivery.Id)!.Notification!;
        string thread = latest.ScopeId is null ? latest.Kind : $"{latest.Kind}|{latest.ScopeId}";
        if (deliveries.Count > 1)
        {
            return new PushMessage(latest.Area, $"{deliveries.Count} new · {latest.Title}", InboxLink, null, thread, thread, badge,
                Urgent: false, Background: false);
        }

        string title = latest.Test ? $"Test · {latest.Title}" : latest.Title;
        return new PushMessage(title, latest.Body, OnThePhone(latest.InAdminPanel ? null : latest.Link, places), latest.Id.ToString(), thread, latest.Subject,
            badge, latest.Urgent, Background: false);
    }

    public static string OnThePhone(string? link, DestinationCatalog places) => link switch
    {
        null => InboxLink,
        _ when LinkTargets.KindOf(link) == LinkKind.InApp && places.Resolve(link) is null => InboxLink,
        _ => link,
    };

    public static void Record(List<Delivery> deliveries, PushResult result, DateTime now)
    {
        foreach (Delivery delivery in deliveries)
        {
            delivery.LeaseUntil = null;
            delivery.LastError = result.Error;
            switch (result.Outcome)
            {
                case PushOutcome.Sent:
                    delivery.SentAt = now;
                    break;
                case PushOutcome.Retry when now - delivery.CreatedAt < GiveUpAfter:
                    delivery.Attempts++;
                    delivery.DueAt = now + Backoff(delivery.Attempts);
                    break;
                default:
                    delivery.GaveUpAt = now;
                    break;
            }
        }
    }

    private static TimeSpan Backoff(int attempts) =>
        TimeSpan.FromMinutes(Math.Min(Math.Pow(2, attempts - 1), LongestBackoff.TotalMinutes));
}
