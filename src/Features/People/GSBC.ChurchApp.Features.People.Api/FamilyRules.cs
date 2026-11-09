using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.People;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.People.Api;

internal static class FamilyRules
{
    public static string? WhyNotUnlink(Guid caller, FamilyLink link, bool childIsMinor, bool callerLinksFamilies) =>
        caller == link.ParentId || callerLinksFamilies || (caller == link.ChildId && !childIsMinor)
            ? null
            : caller == link.ChildId
                ? "A parent or the church office can take this link away while you're under 18."
                : "Only the parent, or the church office, can take this link away.";

    public static string? WhyNotLink(Guid parentId, Guid childId, IReadOnlyCollection<FamilyLink> existing) =>
        parentId == childId ? "Someone can't be their own parent."
        : existing.Any(link => link.ParentId == childId && link.ChildId == parentId) ? "They're already linked the other way round."
        : null;

    public static IReadOnlyList<Guid> LeftAlone(Guid parentId, IReadOnlyCollection<FamilyLink> links, IReadOnlyDictionary<Guid, PersonAccount> accounts) =>
        links.Where(link => link.ParentId == parentId)
            .Select(link => link.ChildId)
            .Where(child => !links.Any(other => other.ChildId == child && other.ParentId != parentId))
            .Where(child => accounts.TryGetValue(child, out PersonAccount? account) && account is { AddedByFamily: true, CanSignIn: false, Deleted: false })
            .ToList();

    public static bool MaySetOwnBirthDate(PersonRecord? existing) => existing?.BirthDate is null;

    public static DateTime? ParentRoleEnds(DateOnly? childBorn) =>
        childBorn is { } born ? ChurchTime.ToUtc(Ages.ComesOfAge(born).ToDateTime(TimeOnly.MinValue)) : null;

    public static DateOnly? TrainingDue(DateOnly? done, DateOnly? due) =>
        due ?? done?.AddYears(BlueCards.TrainingLastsYears);
}
