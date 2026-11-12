using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Rosters.Api;

internal sealed class RosterMessenger(INotifications notifications, IFamilies families, IPeopleIndex people, IMinistryFacts ministries)
{
    public static string Link(Guid ministryId) => $"/ministries/{ministryId}/roster";

    public async Task TellAsync(Guid person, NotificationKind kind, string title, string body, Guid ministryId, string subject, Guid? actor,
        CancellationToken cancellationToken, string? action = null)
    {
        PersonReach reach = await families.ReachAsync(person, cancellationToken);
        if (reach.Directly)
            await notifications.RaiseAsync(new Notification(kind, NotificationAudience.Person(person), title, body, Link(ministryId),
                Subject: subject, Action: action) { Actor = actor }, cancellationToken);
        if (!reach.Minor || reach.Parents.Count == 0)
            return;

        string name = RosterRules.First((await people.NamesAsync([person], cancellationToken)).GetValueOrDefault(person) ?? "your child");
        foreach (Guid parent in reach.Parents)
            await notifications.RaiseAsync(new Notification(kind, NotificationAudience.Person(parent), $"For {name}: {title}", body, Link(ministryId),
                Subject: subject, Action: action) { Actor = actor }, cancellationToken);
    }

    public async Task TellLeadersAsync(Guid ministryId, NotificationKind kind, string title, string body, string subject, Guid? actor,
        CancellationToken cancellationToken)
    {
        foreach (MinistryMember leader in (await ministries.TeamAsync(ministryId, cancellationToken)).Where(member => member.Leads))
            await notifications.RaiseAsync(new Notification(kind, NotificationAudience.Person(leader.PersonId), title, body, Link(ministryId),
                Subject: subject, Action: "See the roster") { Actor = actor }, cancellationToken);
    }

    public Task SettleAsync(string subject, CancellationToken cancellationToken) => notifications.SettleAsync(subject, cancellationToken);
}
