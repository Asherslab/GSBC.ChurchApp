using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.People;

namespace GSBC.ChurchApp.Features.Accounts.Client;

internal sealed class AccountsPeopleDirectory(AccountsClient accounts) : IPeopleDirectory
{
    public async Task<IReadOnlyList<PersonSummary>> SearchAsync(string query) => Map(await accounts.SearchPeopleAsync(query));

    public async Task<IReadOnlyList<PersonSummary>> GetAsync(IReadOnlyCollection<string> ids) =>
        ids.Count == 0 ? [] : Map(await accounts.GetPeopleAsync(ids));

    private static List<PersonSummary> Map(PeopleList people) =>
        people.People.Select(person => new PersonSummary(person.Id, person.DisplayName, person.Email, person.Disabled)).ToList();
}
