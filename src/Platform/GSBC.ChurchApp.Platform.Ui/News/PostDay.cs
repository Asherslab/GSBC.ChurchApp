using System.Globalization;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Platform.Ui.News;

public sealed record PostDay(string Word, string Number, string Month, string Phrase)
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;

    public static PostDay Of(DateTime postedUtc, DateTime nowUtc)
    {
        DateTime day = ChurchTime.ToChurch(postedUtc).Date;
        DateTime today = ChurchTime.ToChurch(nowUtc).Date;
        string longDate = ChurchDates.Describe(postedUtc, DateLineStyle.Long, withTime: false, nowUtc);
        (string word, string phrase) = (today - day).Days switch
        {
            0 => ("Today", "today"),
            1 => ("Yesterday", "yesterday"),
            _ when day.Year != today.Year => (day.ToString("yyyy", English), $"on {longDate}"),
            _ => (day.ToString("ddd", English), $"on {longDate}"),
        };
        return new PostDay(word, day.Day.ToString(English), day.ToString("MMM", English), phrase);
    }
}
