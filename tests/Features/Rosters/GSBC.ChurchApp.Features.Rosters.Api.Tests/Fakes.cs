using GSBC.ChurchApp.Platform.Api.Calendars;
using GSBC.ChurchApp.Platform.Api.Ministries;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Contracts.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Rosters.Api.Tests;

internal sealed class FakeMinistries : IMinistryFacts
{
    public Dictionary<Guid, MinistryFacts> Ministries { get; } = [];
    public Dictionary<Guid, List<MinistryMember>> Teams { get; } = [];

    public ValueTask<MinistryFacts?> FindAsync(Guid ministryId, CancellationToken cancellationToken) => ValueTask.FromResult(Ministries.GetValueOrDefault(ministryId));

    public ValueTask<IReadOnlyList<MinistryFacts>> UsingAsync(string part, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<MinistryFacts>>(Ministries.Values.Where(ministry => ministry.Uses(part)).ToList());

    public ValueTask<IReadOnlyList<MinistryMember>> TeamAsync(Guid ministryId, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<MinistryMember>>(Teams.GetValueOrDefault(ministryId) ?? []);
}

internal sealed class FakeFamilies : IFamilies
{
    public Dictionary<Guid, Guid> ParentOf { get; } = [];

    public ValueTask<PersonReach> ReachAsync(Guid personId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(ParentOf.TryGetValue(personId, out Guid parent)
            ? new PersonReach(personId, Minor: true, Directly: false, Parents: [parent])
            : new PersonReach(personId, Minor: false, Directly: true, Parents: []));

    public ValueTask<bool> MayActForAsync(Guid actorId, Guid personId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(actorId == personId || ParentOf.GetValueOrDefault(personId) == actorId);

    public ValueTask<IReadOnlyDictionary<Guid, FamilyFacts>> FactsAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, FamilyFacts>>(people.Distinct().ToDictionary(person => person, person =>
            ParentOf.TryGetValue(person, out Guid parent)
                ? new FamilyFacts(person, 12, CanSignIn: false, [parent], [])
                : new FamilyFacts(person, null, CanSignIn: true, [], ParentOf.Where(each => each.Value == person).Select(each => each.Key).ToList())));

    public ValueTask LinkAsync(Guid parentId, Guid childId, Guid linkedBy, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

internal sealed class FakePeople : IPeopleIndex
{
    public Dictionary<Guid, string> Names { get; } = [];

    public ValueTask<IReadOnlyList<Guid>> EveryoneAsync(CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<Guid>>(Names.Keys.ToList());

    public ValueTask<IReadOnlyList<IndexedPerson>> FindAsync(string query, int limit, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<IndexedPerson>>([]);

    public ValueTask<IReadOnlyList<IndexedPerson>> GetAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<IndexedPerson>>([]);

    public ValueTask<IReadOnlyDictionary<Guid, string>> NamesAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, string>>(people.Where(Names.ContainsKey).Distinct().ToDictionary(person => person, person => Names[person]));

    public ValueTask<IReadOnlySet<Guid>> NotInvitedYetAsync(IReadOnlyCollection<Guid> people, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
}

internal sealed class NobodyCarded : ISafeguardingStandings
{
    public ValueTask<IReadOnlyDictionary<Guid, SafeguardingStanding>> OfAsync(IReadOnlyCollection<Guid> people, DateOnly on, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<Guid, SafeguardingStanding>>(new Dictionary<Guid, SafeguardingStanding>());
}

internal sealed class FakeEvents : IRosterableEvents
{
    public Dictionary<string, RosterableEvent> Events { get; } = [];

    public ValueTask<IReadOnlyList<RosterableEvent>> VisibleToAsync(Guid personId, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<RosterableEvent>>(Events.Values.ToList());

    public ValueTask<IReadOnlyDictionary<string, RosterableEvent>> FindAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyDictionary<string, RosterableEvent>>(Events.Where(each => keys.Contains(each.Key)).ToDictionary());
}

internal sealed class NoCalendars : IMinistryCalendars
{
    public ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(Guid ministryId, CancellationToken cancellationToken) => ValueTask.FromResult<IReadOnlyList<MinistryCalendar>>([]);

    public ValueTask<IReadOnlyList<MinistryCalendar>> OfAsync(IReadOnlyCollection<Guid> ministryIds, CancellationToken cancellationToken) =>
        ValueTask.FromResult<IReadOnlyList<MinistryCalendar>>([]);

    public ValueTask ShowAsync(Guid ministryId, bool shown, Audience whileHidden, CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask<MinistryCalendar> CreateAsync(Guid ministryId, string name, Audience audience, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask<MinistryCalendar> ChangeAsync(Guid ministryId, Guid calendarId, string name, Audience audience, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public ValueTask<MinistryCalendarRemoval> WhatGoesAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public ValueTask RemoveAsync(Guid ministryId, Guid calendarId, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

internal sealed class SentNotifications : INotifications
{
    public List<Notification> Sent { get; } = [];

    public Task RaiseAsync(Notification notification, CancellationToken cancellationToken)
    {
        Sent.Add(NotificationLinks.Checked(notification));
        return Task.CompletedTask;
    }

    public Task RaiseOneOfAsync(IReadOnlyList<Notification> mostSpecificFirst, CancellationToken cancellationToken) => RaiseAsync(mostSpecificFirst[0], cancellationToken);

    public Task SettleAsync(string subject, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task CollectAsync(NotificationDigestItem item, CancellationToken cancellationToken) => Task.CompletedTask;

    public Task ForgetDigestAsync(NotificationKind kind, string group, CancellationToken cancellationToken) => Task.CompletedTask;
}
