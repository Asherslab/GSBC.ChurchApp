namespace GSBC.ChurchApp.Platform.Client.Destinations;

public sealed record LinkIntoTheApp(string Path)
{
    public string Route => Path.Trim('/');
}
