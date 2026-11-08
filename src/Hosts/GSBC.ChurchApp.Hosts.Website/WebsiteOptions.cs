namespace GSBC.ChurchApp.Hosts.Website;

public sealed class WebsiteOptions
{
    public const string Section = "Website";

    public string EventsPage { get; set; } = "";
    public string SermonsPage { get; set; } = "";
    public string AppOrigin { get; set; } = "";
    public string PodcastLink { get; set; } = "";
    public string FrameAncestors { get; set; } = "";
}
