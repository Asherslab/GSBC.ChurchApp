using GSBC.ChurchApp.Features.Rosters.Contracts;

namespace GSBC.ChurchApp.Features.Rosters.Client;

public static class TeamAway
{
    public static bool Shown(RosterView view) => view is { MayEdit: true, Archived: false, Team.Count: > 0 };

    public static IReadOnlyList<PersonLine> People(RosterView view) =>
        view.You.Concat(view.Team.Select(member => new PersonLine { Id = member.PersonId, Name = member.Name }))
            .DistinctBy(person => person.Id).ToList();

    public static IEnumerable<AwayOf> Spans(RosterView view) =>
        view.Team.Select(member => new AwayOf { PersonId = member.PersonId, Name = member.Name, Spans = member.Away });

    public static async Task<AwayList> LoadAsync(IRosterReads reads, RosterView view)
    {
        AwayList yours = await reads.AwayAsync();
        return new AwayList
        {
            YourId = view.YourId,
            People = yours.People.Concat(Spans(view)).DistinctBy(person => person.PersonId).ToList(),
        };
    }
}
