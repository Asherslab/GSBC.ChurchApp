using System.Linq.Expressions;
using GSBC.ChurchApp.Features.Notifications.Api.Data;
using GSBC.ChurchApp.Features.Notifications.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Contracts.Notifications;

namespace GSBC.ChurchApp.Features.Notifications.Api;

internal sealed record Recipients(IReadOnlyList<Guid> People, IReadOnlyList<Guid> Phones, bool EveryPhone, bool Public)
{
    public Expression<Func<Device, bool>> Devices()
    {
        List<Guid> peopleIds = [.. People];
        List<Guid> phoneIds = [.. Phones];
        bool everyPhone = EveryPhone;
        return device => (device.UserId != null && peopleIds.Contains(device.UserId.Value))
            || (device.UserId == null && (everyPhone || phoneIds.Contains(device.InstallId)));
    }

    private HashSet<Guid>? _people;
    private HashSet<Guid>? _phones;

    public bool Reach(Guid person) => (_people ??= [.. People]).Contains(person);

    public bool Reach(Device device) => device.UserId is { } userId ? Reach(userId) : EveryPhone || (_phones ??= [.. Phones]).Contains(device.InstallId);

    public static Recipients Together(IReadOnlyCollection<Recipients> each) => new(
        each.SelectMany(recipients => recipients.People).Distinct().ToList(),
        each.SelectMany(recipients => recipients.Phones).Distinct().ToList(),
        each.Any(recipients => recipients.EveryPhone),
        each.Any(recipients => recipients.Public));
}

internal static class NotificationRules
{
    private static readonly TimeSpan SummaryPause = TimeSpan.FromMinutes(10);

    public static NotificationKind Validated(NotificationKindCatalog kinds, Notification notification)
    {
        if (!kinds.ByKey.TryGetValue(notification.Kind.Key, out NotificationKind? kind))
            throw new InvalidOperationException($"The notification kind '{notification.Kind.Key}' isn't registered.");
        if (notification.Subject is not null && LinkTargets.InAppRoute(notification.Link ?? "") is null)
            throw new ArgumentException($"A notification about '{notification.Subject}' must link to it in the app.", nameof(notification));
        if (notification.Link is { } link && !LinkTargets.IsSafe(link))
            throw new ArgumentException($"'{link}' isn't a safe link.", nameof(notification));
        NotificationLinks.Checked(notification);
        if (notification.Urgent && !kind.MayBeUrgent)
            throw new ArgumentException($"'{kind.Key}' notifications can't be urgent.", nameof(notification));
        return kind;
    }

    public static async Task<Recipients> RecipientsAsync(
        Notification notification, IPermissionHolders holders, IPeopleIndex people, CancellationToken cancellationToken)
    {
        if (notification is { Test: true, Audience: not NotificationAudience.PersonAudience })
            throw new ArgumentException("A test goes to one person.", nameof(notification));

        Recipients found = await AudienceAsync(notification, holders, people, cancellationToken);
        IReadOnlySet<Guid> notInvitedYet = await people.NotInvitedYetAsync(found.People, cancellationToken);
        Guid? actor = notification.Test ? null : notification.Actor;
        return found with { People = found.People.Where(person => person != actor && !notInvitedYet.Contains(person)).ToList() };
    }

    private static async Task<Recipients> AudienceAsync(
        Notification notification, IPermissionHolders holders, IPeopleIndex people, CancellationToken cancellationToken)
    {
        switch (notification.Audience)
        {
            case NotificationAudience.EveryoneAudience:
                return new Recipients(await people.EveryoneAsync(cancellationToken), [], EveryPhone: true, Public: true);
            case NotificationAudience.SignedInAudience:
                return new Recipients(await people.EveryoneAsync(cancellationToken), [], EveryPhone: false, Public: false);
            case NotificationAudience.PersonAudience person:
                return new Recipients([person.UserId], [], EveryPhone: false, Public: false);
            case NotificationAudience.HoldingAudience holding:
                PermissionHolders found = await holders.HoldersAsync(holding.Permission, holding.ObjectId, cancellationToken);
                return new Recipients(found.People, found.Phones, EveryPhone: false, Public: found.Phones.Count > 0);
            default:
                throw new ArgumentException("Unknown audience.", nameof(notification));
        }
    }

    public static bool Wanted(NotificationKind kind, string? scopeId, Device device, IReadOnlyCollection<Preference> preferences) =>
        device.UserId is { } userId
            ? WantedBy(kind, scopeId, userId, preferences)
            : Wanted(kind, scopeId, preference => preference.InstallId == device.InstallId, preferences);

    public static bool WantedBy(NotificationKind kind, string? scopeId, Guid userId, IReadOnlyCollection<Preference> preferences) =>
        Wanted(kind, scopeId, preference => preference.UserId == userId, preferences);

    private static bool Wanted(NotificationKind kind, string? scopeId, Func<Preference, bool> mine, IReadOnlyCollection<Preference> preferences)
    {
        if (kind.Mandatory)
            return true;
        List<Preference> list = preferences.Where(preference => preference.Kind == kind.Key && mine(preference)).ToList();
        Preference? chosen = list.FirstOrDefault(preference => scopeId is not null && preference.ScopeId == scopeId)
            ?? list.FirstOrDefault(preference => preference.ScopeId is null);
        return chosen?.Push ?? kind.DefaultPush;
    }

    public static (bool Summarised, DateTime DueAt) Scheduled(NotificationKind kind, Notification notification, int recentPushes, DateTime now)
    {
        bool overLimit = kind.Pacing is { } limit && recentPushes >= limit.Pushes;
        DateTime due = overLimit ? now + SummaryPause : now;
        bool interrupts = notification.Urgent || kind.Mandatory || notification.Test;
        return (overLimit, interrupts ? due : AfterQuietHours(due));
    }

    private static DateTime AfterQuietHours(DateTime utc) =>
        QuietHours.HeldUntil(ChurchTime.ToChurch(utc)) is { } morning ? ChurchTime.ToUtc(morning) : utc;
}
