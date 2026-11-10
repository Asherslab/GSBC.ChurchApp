using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Client.Modules;

public sealed record MinistryChatsPlace(string MinistryId, string Name, bool MayStart, bool HasAttendees);

public sealed record MinistryChatsView(Func<MinistryChatsPlace, RenderFragment> Area, Func<AskInChat, Task> AskAsync);

public sealed record AskInChat(string MinistryId, string PersonId, string Place, string Text);

public interface IMinistryPeopleWrites
{
    Task AddAsync(string ministryId, string personId, string place);
}
