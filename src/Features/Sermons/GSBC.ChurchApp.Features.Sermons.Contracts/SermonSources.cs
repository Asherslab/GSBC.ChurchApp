namespace GSBC.ChurchApp.Features.Sermons.Contracts;

public static class SermonSources
{
    public const string Website = "website";
    public const string Uploads = "uploads";

    public static bool IsKnown(string source) => source is Website or Uploads;
}
