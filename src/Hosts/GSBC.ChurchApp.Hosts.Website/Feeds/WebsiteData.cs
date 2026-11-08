using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Features.Events.Contracts;
using GSBC.ChurchApp.Features.Sermons.Client;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Hosts.Website.Feeds;

public sealed record WebsiteData(SermonShelf Sermons, IReadOnlyList<CalendarInfo> Calendars, IReadOnlyList<EventOccurrence> Events)
{
    public static readonly IReadOnlyList<string> Feeds =
        [SermonsFeeds.Catalogue, SermonsFeeds.Series, SermonsFeeds.Speakers, EventsFeeds.Calendars, EventsFeeds.Public];

    public static readonly WebsiteData Empty = new(SermonShelf.Empty, [], []);

    public CalendarInfo? Calendar(string id) => Calendars.FirstOrDefault(calendar => calendar.Id == id);

    public EventOccurrence? Event(string id) => Events.FirstOrDefault(occurrence => occurrence.Id == id);

    public static WebsiteData From(FeedMirror mirror) => new(
        new SermonShelf(
            Read<Sermon>(mirror, SermonsFeeds.Catalogue, SermonsContractsModel.Instance).OrderByDescending(sermon => sermon.PreachedAt).ToList(),
            Read<SeriesInfo>(mirror, SermonsFeeds.Series, SermonsContractsModel.Instance).OrderByDescending(series => series.LastAt).ToList(),
            Read<SpeakerInfo>(mirror, SermonsFeeds.Speakers, SermonsContractsModel.Instance).OrderBy(speaker => speaker.Name, StringComparer.OrdinalIgnoreCase).ToList()),
        Read<CalendarInfo>(mirror, EventsFeeds.Calendars, EventsContractsModel.Instance).OrderBy(calendar => calendar.Order).ThenBy(calendar => calendar.Name, StringComparer.OrdinalIgnoreCase).ToList(),
        Read<EventOccurrence>(mirror, EventsFeeds.Public, EventsContractsModel.Instance).OrderBy(occurrence => occurrence.StartsAt).ThenBy(occurrence => occurrence.Fields.Title, StringComparer.OrdinalIgnoreCase).ToList());

    private static IEnumerable<T> Read<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(FeedMirror mirror, string feed, TypeModel model) =>
        mirror.Rows(feed).Select(row => model.Deserialize<T>(new MemoryStream(row.Payload)));
}
