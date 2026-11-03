using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Contracts.Destinations;

namespace GSBC.ChurchApp.Platform.Ui.Destinations;

public static class PlaceWords
{
    public static string Subtitle(Destination place, DateTime nowUtc) =>
        place.When is { } when ? $"{place.Where} · {ChurchDates.Describe(when, DateLineStyle.Short, withTime: false, nowUtc)}" : place.Where;

    public static string Short(Destination place) => place.Audience switch
    {
        _ when place.Audience == Audience.Everyone.Text => "Everyone",
        _ when place.Audience == Audience.SignedIn.Text => "Signed in",
        _ => "Some people",
    };

    public static string Unavailable(DestinationState state) => state switch
    {
        DestinationState.NotForYou => "You can't open this place, so it can't be shown here.",
        DestinationState.Gone => "It has finished, or the church has taken it down.",
        _ => "That isn't a place in the app any more.",
    };
}
