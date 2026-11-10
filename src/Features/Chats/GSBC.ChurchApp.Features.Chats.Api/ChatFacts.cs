using GSBC.ChurchApp.Platform.Contracts.Chats;
using GSBC.ChurchApp.Features.Chats.Api.Data;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Chats;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.Chats.Api;

internal sealed class ChatFacts(
    ChatsDbContext db, IChatPlaces places, IPermissionHolders holders, IAccessEvaluator access, IRoleAssignments roles, IFamilies families,
    IBlueCards blueCards, TimeProvider time)
{
    private readonly Dictionary<(string Node, string Scope), IReadOnlyList<Guid>> _holders = [];
    private IReadOnlySet<Guid>? _paused;

    public async Task<ChatPlace?> PlaceAsync(Guid ministryId, CancellationToken cancellationToken) =>
        (await places.PlacesAsync([ministryId], cancellationToken)).GetValueOrDefault(ministryId);

    public void Forget()
    {
        _holders.Clear();
        _paused = null;
    }

    public async Task<IReadOnlySet<Guid>> PausedAsync(CancellationToken cancellationToken) =>
        _paused ??= (await roles.HoldersAsync(ChatsPermissions.PausedRole, null, cancellationToken)).Select(holder => holder.UserId).ToHashSet();

    public async Task<MembershipFacts> ForAsync(ChatRecord chat, ChatPlace? place, CancellationToken cancellationToken)
    {
        bool open = chat.DeletedAt is null && place is { ChatsOn: true };
        if (!open || place is null)
            return new MembershipFacts(false, false, [], [], new Dictionary<Guid, PersonFacts>(), new Dictionary<Guid, bool?>(), new HashSet<Guid>(), new HashSet<Guid>());

        IReadOnlyList<Guid> inAudience = ChatAudienceRules.UsesChosenPeople(chat)
            ? (await roles.HoldersAsync(ChatsPermissions.MemberRole, chat.Id.ToString(), cancellationToken)).Select(holder => holder.UserId).ToList()
            : place.Audiences.TryGetValue(chat.Audience, out Audience? audience) && audience is { Node: { } node, At: { } at }
                ? await HoldersAsync(new Permission(node, at.Kind, node), at.ObjectId!, cancellationToken)
                : [];
        IReadOnlyList<Guid> leaders = await HoldersAsync(ChatsPermissions.Manage, chat.MinistryId.ToString(), cancellationToken);

        List<Guid> everyone = inAudience.Concat(leaders).Distinct().ToList();
        IReadOnlyDictionary<Guid, FamilyFacts> family = await families.FactsAsync(everyone, cancellationToken);
        Dictionary<Guid, PersonFacts> people = family.Values.ToDictionary(facts => facts.PersonId, facts => new PersonFacts(facts.PersonId, facts.Minor, facts.Parents));
        List<Guid> parents = people.Values.Where(person => person.Minor).SelectMany(person => person.Parents).ToList();
        Dictionary<Guid, bool?> consents = await db.Consents.AsNoTracking().Where(consent => consent.ChatId == chat.Id)
            .ToDictionaryAsync(consent => consent.ChildId, consent => consent.Approved, cancellationToken);
        DateOnly today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        return new MembershipFacts(
            true, chat.Kind == ChatKinds.Ask, inAudience, leaders, people, consents, await PausedAsync(cancellationToken),
            await blueCards.HoldingCurrentAsync(everyone.Concat(parents).Distinct().ToList(), today, cancellationToken));
    }

    public async Task<bool> IsAllowedAsync(Guid userId, Permission permission, string? objectId, CancellationToken cancellationToken) =>
        (await access.GrantsForAsync(userId, cancellationToken)).IsAllowed(permission, objectId);

    private async Task<IReadOnlyList<Guid>> HoldersAsync(Permission permission, string objectId, CancellationToken cancellationToken)
    {
        if (_holders.TryGetValue((permission.Node, objectId), out IReadOnlyList<Guid>? known))
            return known;

        List<Guid> here = [];
        foreach (Guid person in (await holders.HoldersAsync(permission, objectId, cancellationToken)).People)
        {
            if (HeldThere(await access.GrantsForAsync(person, cancellationToken), permission, objectId))
                here.Add(person);
        }
        return _holders[(permission.Node, objectId)] = here;
    }

    internal static bool HeldThere(GrantSet grants, Permission permission, string objectId) =>
        new GrantSet(grants.Grants.Where(grant => !grant.Scope.IsGlobal).ToList()).IsAllowed(permission, objectId);
}

internal static class ChatAudienceRules
{
    public static bool UsesChosenPeople(ChatRecord chat) => chat.Kind == ChatKinds.Ask || chat.Audience == ChatAudiences.Chosen;

    public static bool IsValid(string audience, ChatPlace place) => audience == ChatAudiences.Chosen || place.Audiences.ContainsKey(audience);
}
