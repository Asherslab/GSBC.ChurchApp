namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record ChatAsks(Func<AskInChat, Task> AskAsync);

public sealed record AskInChat(string MinistryId, string PersonId, string Place, string Text);

public interface IMinistryPeopleWrites
{
    Task AddAsync(string ministryId, string personId, string place);
}
