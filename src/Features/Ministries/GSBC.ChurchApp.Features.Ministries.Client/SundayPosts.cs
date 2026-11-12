using System.Globalization;
using GSBC.ChurchApp.Features.Ministries.Contracts;
using GSBC.ChurchApp.Platform.Client.Modules;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class SundayPosts
{
    public const string NoClass = "No class this Sunday";

    public static IReadOnlyList<string> UpcomingSundays(MinistryPage page, TimeProvider clock)
    {
        string today = ChurchTime.Today(clock).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return page.Sundays.Where(sunday => string.CompareOrdinal(sunday, today) >= 0).ToList();
    }

    public static IReadOnlyList<DayNote> Notes(MinistryPage page) =>
        page.Posts.Where(post => post.Sunday.Length > 0 && post.From.Length == 0)
            .Select(post => new DayNote(LifeGroupRounds.Day(post.Sunday), $"News: {post.Title}", Href(page.Ministry.Id, post.Id), post.NoClass ? NoClass : null))
            .ToList();

    public static string Href(string ministryId, string postId) => $"{MinistriesPlaces.Of(ministryId, MinistriesPlaces.News)}#post-{postId}";

    public static string For(Post post) => post.Sunday.Length == 0 ? "" : $"For Sunday {MinistryWords.Day(post.Sunday)}";
}
