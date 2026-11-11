using Grpc.Core;
using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.Access;
using GSBC.ChurchApp.Platform.Api.Notifications;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.EntityFrameworkCore;

namespace GSBC.ChurchApp.Features.People.Api;

internal sealed class Families(PeopleDbContext db, IPeopleAccounts accounts, IRoleAssignments roles, INotifications notifications, TimeProvider time)
{
    private DateTime Now => time.GetUtcNow().UtcDateTime;

    public async Task LinkAsync(Guid parentId, Guid childId, string how, Guid? linkedBy, CancellationToken cancellationToken)
    {
        List<FamilyLink> existing = await db.Links.Where(link => link.ChildId == parentId || link.ChildId == childId).ToListAsync(cancellationToken);
        if (FamilyRules.WhyNotLink(parentId, childId, existing) is { } problem)
            throw new RpcException(new Status(StatusCode.FailedPrecondition, problem));

        IReadOnlyDictionary<Guid, PersonAccount> people = await accounts.GetAsync([parentId, childId], cancellationToken);
        if (!people.TryGetValue(parentId, out PersonAccount? parent) || parent.Deleted || !people.TryGetValue(childId, out PersonAccount? child) || child.Deleted)
            throw new RpcException(new Status(StatusCode.NotFound, "That person isn't in the app any more."));

        if (!existing.Any(link => link.ParentId == parentId && link.ChildId == childId))
            db.Links.Add(new FamilyLink { ChildId = childId, ParentId = parentId, How = how, LinkedAt = Now, LinkedBy = linkedBy });
        await TouchAsync([parentId, childId], cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await GiveParentRoleAsync(parentId, childId, cancellationToken);

        if (child.CanSignIn && linkedBy != childId)
            await notifications.RaiseAsync(new Notification(PeopleNotifications.Linked, NotificationAudience.Person(childId),
                $"{parent.Name} is linked as your parent", "They can see your details in the app while you're under 18.", "/account",
                Subject: $"people-linked-{parentId:N}-{childId:N}") { Actor = linkedBy }, cancellationToken);
    }

    public async Task UnlinkAsync(FamilyLink link, CancellationToken cancellationToken)
    {
        db.Links.Remove(link);
        await TouchAsync([link.ParentId, link.ChildId], cancellationToken: cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await roles.TakeAwayAsFeatureAsync(link.ParentId, PeoplePermissions.ParentRole, link.ChildId.ToString(), cancellationToken);
    }

    public async Task RefreshParentRolesAsync(Guid childId, CancellationToken cancellationToken)
    {
        foreach (Guid parentId in await db.Links.Where(link => link.ChildId == childId).Select(link => link.ParentId).ToListAsync(cancellationToken))
            await GiveParentRoleAsync(parentId, childId, cancellationToken);
    }

    public async Task TouchAsync(IReadOnlyCollection<Guid> people, bool safeguarding = false, CancellationToken cancellationToken = default)
    {
        List<Guid> parents = await db.Links.Where(link => people.Contains(link.ChildId)).Select(link => link.ParentId).ToListAsync(cancellationToken);
        DateTime now = Now;
        foreach (Guid person in people.Concat(parents).Distinct())
        {
            string id = person.ToString();
            db.FeedChanges.Add(Marker(PeopleFeeds.Mine, id, now));
            db.FeedChanges.Add(Marker(PeopleFeeds.Family, id, now));
        }
        if (safeguarding)
            foreach (Guid person in people)
                db.FeedChanges.Add(Marker(PeopleFeeds.Safeguarding, person.ToString(), now));
    }

    private async Task GiveParentRoleAsync(Guid parentId, Guid childId, CancellationToken cancellationToken)
    {
        DateOnly? born = (await db.People.AsNoTracking().SingleOrDefaultAsync(person => person.UserId == childId, cancellationToken))?.BirthDate;
        DateTime? ends = FamilyRules.ParentRoleEnds(born);
        if (ends is { } end && end <= Now)
            await roles.TakeAwayAsFeatureAsync(parentId, PeoplePermissions.ParentRole, childId.ToString(), cancellationToken);
        else
            await roles.GiveAsFeatureAsync(parentId, PeoplePermissions.ParentRole, childId.ToString(), ends, cancellationToken);
    }

    private static FeedChangeRecord Marker(string feed, string person, DateTime now) =>
        new() { Feed = feed, RecordId = person, ScopeId = person, Version = now.Ticks };
}
