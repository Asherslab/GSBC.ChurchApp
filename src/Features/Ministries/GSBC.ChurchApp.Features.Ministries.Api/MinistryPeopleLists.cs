using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Api.People;

namespace GSBC.ChurchApp.Features.Ministries.Api;

internal sealed record MinistryHoldings(IReadOnlyDictionary<(string Role, Guid Place), IReadOnlyList<Guid>> Holders)
{
    public static readonly IReadOnlyList<string> AllRoles = [Roles.Leader, Roles.Admin, Roles.Team, Roles.Attendee];

    public IReadOnlyList<Guid> Of(string role, Guid place) => Holders.GetValueOrDefault((role, place)) ?? [];

    public IEnumerable<Guid> People => Holders.Values.SelectMany(holders => holders).Distinct();

    public IReadOnlyList<string> RolesOf(Guid person, Guid place) => AllRoles.Where(role => Of(role, place).Contains(person)).ToList();

    public static MinistryHoldings From(IEnumerable<(string Role, Guid Person, Guid Place)> holdings) =>
        new(holdings.GroupBy(holding => (holding.Role, holding.Place))
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Guid>)group.Select(holding => holding.Person).Distinct().ToList()));
}

internal static class MinistryPeopleLists
{
    public static IReadOnlyList<Guid> Everyone(MinistryRecord ministry, MinistryTree tree, MinistryHoldings holdings) =>
        MinistryHoldings.AllRoles.SelectMany(role => holdings.Of(role, ministry.Id))
            .Concat(tree.Above(ministry.Id).SelectMany(above => holdings.Of(Roles.Leader, above.Id)))
            .Distinct()
            .ToList();

    public static PeopleList Build(
        MinistryRecord ministry, MinistryTree tree, MinistryHoldings holdings, IReadOnlyDictionary<Guid, FamilyFacts> facts,
        IReadOnlyDictionary<Guid, string> names, IReadOnlyList<AskLine> asks, IReadOnlyList<string> mayGive)
    {
        List<(Guid Person, string Role, string From)> team = [];
        foreach (string role in new[] { Roles.Leader, Roles.Admin, Roles.Team })
            team.AddRange(holdings.Of(role, ministry.Id).Select(person => (person, role, "")));
        foreach (MinistryRecord above in tree.Above(ministry.Id))
            team.AddRange(holdings.Of(Roles.Leader, above.Id).Select(person => (person, Roles.Leader, above.Name)));
        team = team.GroupBy(member => member.Person).Select(member => member.OrderBy(each => RoleOrder(each.Role)).ThenBy(each => each.From.Length).First()).ToList();
        IReadOnlyList<Guid> attendees = holdings.Of(Roles.Attendee, ministry.Id);

        Member MemberOf(Guid person, string role, string from)
        {
            FamilyFacts? fact = facts.GetValueOrDefault(person);
            return new Member
            {
                PersonId = person.ToString(), Name = names.GetValueOrDefault(person) ?? "Someone", Role = role, Age = fact?.Minor == true ? fact.Age : null,
                Parents = (fact?.Parents ?? []).Select(parent => new PersonLine { Id = parent.ToString(), Name = names.GetValueOrDefault(parent) ?? "A parent" }).ToList(),
                NoParent = fact is { Minor: true, Parents.Count: 0 }, CanSignIn = fact?.CanSignIn ?? true, LeadsFrom = from,
            };
        }

        return new PeopleList
        {
            Team = team.Select(member => MemberOf(member.Person, member.Role, member.From))
                .OrderBy(member => RoleOrder(member.Role)).ThenBy(member => member.Name, StringComparer.CurrentCulture).ToList(),
            Attendees = attendees.Select(person => MemberOf(person, Roles.Attendee, ""))
                .OrderByDescending(member => member.NoParent).ThenBy(member => member.Name, StringComparer.CurrentCulture).ToList(),
            Asks = [.. asks],
            MayGive = [.. mayGive],
            Attending = ministry.AttendJoin != Joining.None || attendees.Count > 0,
        };
    }

    private static int RoleOrder(string role) => role switch { Roles.Leader => 0, Roles.Admin => 1, _ => 2 };
}
