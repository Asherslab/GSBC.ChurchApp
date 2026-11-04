namespace GSBC.ChurchApp.Features.Sermons.Api;

public sealed class WebsiteOptions
{
    public const string Section = "Sermons:Website";

    public string BaseUrl { get; set; } = "https://baptist.com.au";
    public string FeedPath { get; set; } = "/podcasts/media.rss";
    public TimeSpan Every { get; set; } = TimeSpan.FromHours(1);
    public TimeSpan SeriesEvery { get; set; } = TimeSpan.FromDays(1);
    public bool Enabled { get; set; } = true;
}
