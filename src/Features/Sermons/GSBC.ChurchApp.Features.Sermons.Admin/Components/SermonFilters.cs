namespace GSBC.ChurchApp.Features.Sermons.Admin.Components;

public static class SermonFilters
{
    public const string All = "";
    public const string NoSeries = "no-series";
    public const string Hidden = "hidden";
    public const string NeedsALook = "needs-a-look";
}

public sealed record SermonSaved(Contracts.SermonDetail Detail, bool Next);
