using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public sealed class StoredLinkableCalendars(IEventChoices events, IMinistryReads ministries) : ILinkableCalendars
{
    public async Task<IReadOnlyList<Crumb>> ChoicesAsync(string ministryId)
    {
        MinistryPage page = await ministries.MinistryAsync(ministryId);
        HashSet<string> linked = page.Calendars.Where(calendar => calendar.Linked).Select(calendar => calendar.Id).ToHashSet();
        return (await events.ForEveryoneAsync())
            .Where(calendar => calendar.MinistryId != ministryId && !linked.Contains(calendar.Id))
            .Select(calendar => new Crumb { Id = calendar.Id, Name = calendar.Name })
            .ToList();
    }
}
