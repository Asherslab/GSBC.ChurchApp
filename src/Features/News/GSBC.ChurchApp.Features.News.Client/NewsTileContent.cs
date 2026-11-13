using GSBC.ChurchApp.Platform.Client.News;
using GSBC.ChurchApp.Platform.Ui.News;

namespace GSBC.ChurchApp.Features.News.Client;

public sealed record NewsTileContent(IReadOnlyList<NewsItem> UrgentToday, IReadOnlyList<NewsItem> Newest, int New)
{
    public static readonly TimeSpan NewBeforeFirstLook = TimeSpan.FromDays(7);

    public NewsItem? Urgent => UrgentToday.FirstOrDefault();

    public NewsItem? Lead => Newest.FirstOrDefault();

    public bool IsEmpty => UrgentToday.Count == 0 && Newest.Count == 0;

    public static NewsTileContent Of(IEnumerable<NewsItem> items, DateTime nowUtc, DateTime? lastLookedUtc)
    {
        NewsArrangement arranged = NewsArrangement.Of(items, nowUtc);
        List<NewsItem> newest = arranged.Lead is { } lead ? [lead, .. arranged.Rest] : [];
        DateTime since = lastLookedUtc ?? nowUtc - NewBeforeFirstLook;
        return new NewsTileContent(arranged.Today, newest, newest.Count(item => item.PostedAt > since));
    }
}
