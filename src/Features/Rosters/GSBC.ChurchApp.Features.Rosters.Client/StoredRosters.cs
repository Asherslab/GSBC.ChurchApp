using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Modules;

namespace GSBC.ChurchApp.Features.Rosters.Client;

public sealed class StoredRosters(ILocalFeeds feeds, TimeProvider time, IServiceProvider services) : IRosterReads
{
    private const int Page = 200;
    private static readonly TimeSpan ChoicesAhead = TimeSpan.FromDays(13 * 7);

    private readonly IEventChoices? _events = services.GetService(typeof(IEventChoices)) as IEventChoices;

    public IReadOnlyList<string> Feeds => [RostersFeeds.Ministries, RostersFeeds.Away, .. _events?.Feeds ?? []];

    public async Task<RosterView> RosterAsync(string ministryId) =>
        (await YoursAsync()).FirstOrDefault(view => view.MinistryId == ministryId) ?? throw new RosterNotHereException();

    public async Task<IReadOnlyList<RosterView>> YoursAsync()
    {
        List<RosterView> views = [];
        for (int offset = 0; ; offset += Page)
        {
            IReadOnlyList<StoredRecord> page = await feeds.ReadPageAsync(new FeedPageQuery(RostersFeeds.Ministries, Limit: Page, Offset: offset));
            views.AddRange(page.Select(record => RostersContractsModel.Instance.Deserialize<RosterView>(new MemoryStream(record.Payload))));
            if (page.Count < Page)
                return views;
        }
    }

    public async Task<AwayList> AwayAsync() =>
        (await feeds.ReadPageAsync(new FeedPageQuery(RostersFeeds.Away, Limit: 1))).FirstOrDefault() is { } record ? RostersContractsModel.Instance.Deserialize<AwayList>(new MemoryStream(record.Payload)) : new AwayList();

    public async Task<IReadOnlyList<EventChoice>> EventChoicesAsync(string ministryId)
    {
        if (_events is null)
            return [];
        DateTime now = time.GetUtcNow().UtcDateTime;
        return (await _events.UpcomingAsync(now, now + ChoicesAhead))
            .Select(line => new EventChoice
            {
                Key = line.Key, Title = line.Title, CalendarName = line.CalendarName, StartsAt = line.StartsAt, EndsAt = line.EndsAt, AllDay = line.AllDay,
                Own = line.MinistryId == ministryId,
            })
            .ToList();
    }
}
