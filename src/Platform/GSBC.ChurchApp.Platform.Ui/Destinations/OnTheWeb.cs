using GSBC.ChurchApp.Platform.Client.Destinations;

namespace GSBC.ChurchApp.Platform.Ui.Destinations;

public sealed class OnTheWeb(ChurchWebsite website)
{
    public string? AddressOf(string? appHref) => appHref is null ? null : website.AddressOf(appHref);
}
