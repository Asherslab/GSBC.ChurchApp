namespace GSBC.ChurchApp.Features.Sermons.Import.Api;

public sealed class ImportOptions
{
    public const string Section = "Sermons:Import";

    public string WebsiteUrl { get; set; } = "https://baptist.com.au";
    public TimeSpan PageDelay { get; set; } = TimeSpan.FromSeconds(1);
}
