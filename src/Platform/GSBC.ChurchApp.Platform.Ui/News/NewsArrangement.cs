using GSBC.ChurchApp.Platform.Client.News;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Platform.Ui.News;

public sealed record NewsArrangement(IReadOnlyList<NewsItem> Today, NewsItem? Lead, IReadOnlyList<NewsItem> Rest)
{
    public bool IsEmpty => Today.Count == 0 && Lead is null;

    public static NewsArrangement Of(IEnumerable<NewsItem> items, DateTime now)
    {
        List<NewsItem> newestFirst = items.OrderByDescending(item => item.PostedAt).ToList();
        DateOnly today = ChurchTime.DayOf(now);
        List<NewsItem> urgentToday = newestFirst.Where(item => item.Urgent && ChurchTime.DayOf(item.PostedAt) == today).ToList();
        List<NewsItem> others = newestFirst.Except(urgentToday).ToList();
        return new NewsArrangement(urgentToday, others.FirstOrDefault(), others.Skip(1).ToList());
    }
}
