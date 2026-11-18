using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Locations;
using GSBC.ChurchApp.Platform.Ui.Locations;

namespace GSBC.ChurchApp.Features.Events.Client;

public sealed class EventLocations(CalendarDiary diary) : ILocationSource
{
    public IReadOnlyCollection<string> Feeds { get; } = [EventsFeeds.Public, EventsFeeds.Private];

    public async Task<IReadOnlyList<Location>> LocationsAsync() =>
        diary.IsOnThisDevice ? [.. (await diary.LoadAsync()).Occurrences.Select(occurrence => occurrence.Fields.Place?.Location).OfType<Location>()] : [];
}
