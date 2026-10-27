namespace Gsbc.AppPoc.Layouts.Today;

public enum ChurchMoment { Live, Sunday, AfterSunday, Midweek }

public enum FeedBlock { NowPlaying, LiveOutline, Notes, Bulletin, SundaySermons, ComingUp, LifeGroups, LatestSermon, WhatsNext, Prayer, Give }

public static class TodayFeed
{
    private static readonly FeedBlock[] Tail = [FeedBlock.ComingUp, FeedBlock.LifeGroups, FeedBlock.WhatsNext, FeedBlock.Prayer, FeedBlock.Give];

    public static ChurchMoment MomentAt(DateTime now, bool serviceStreaming) => (serviceStreaming, now.DayOfWeek) switch
    {
        (true, _) => ChurchMoment.Live,
        (_, DayOfWeek.Sunday) => ChurchMoment.Sunday,
        (_, DayOfWeek.Monday or DayOfWeek.Tuesday) => ChurchMoment.AfterSunday,
        _ => ChurchMoment.Midweek,
    };

    public static IReadOnlyList<FeedBlock> Order(ChurchMoment moment)
    {
        FeedBlock[] lead = moment switch
        {
            ChurchMoment.Live => [FeedBlock.LiveOutline, FeedBlock.Notes],
            ChurchMoment.Sunday => [FeedBlock.LifeGroups, FeedBlock.Bulletin, FeedBlock.SundaySermons],
            ChurchMoment.AfterSunday => [FeedBlock.SundaySermons, FeedBlock.Bulletin],
            _ => [FeedBlock.ComingUp, FeedBlock.LatestSermon],
        };
        return [FeedBlock.NowPlaying, .. lead, .. Tail.Except(lead)];
    }
}
