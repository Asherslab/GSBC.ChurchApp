namespace GSBC.ChurchApp.Platform.Client.People;

public sealed record PersonSummary(string Id, string Name, string Email, bool Disabled);

public interface IPeopleDirectory
{
    Task<IReadOnlyList<PersonSummary>> SearchAsync(string query);

    Task<IReadOnlyList<PersonSummary>> GetAsync(IReadOnlyCollection<string> ids);
}
