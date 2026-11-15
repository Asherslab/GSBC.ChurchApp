namespace GSBC.ChurchApp.Features.Outreach.Admin.Map;

public enum OutreachMapJob
{
    Coverage,
    Places,
    SavedAreas,
    Reports,
    MapAreas,
    Plan,
}

public enum OutreachLayer
{
    Coverage,
    SkippedPlaces,
    Marks,
    SavedAreas,
    Reports,
}

public static class OutreachMapJobs
{
    public static OutreachMapJob? Of(string? slug) => slug switch
    {
        "coverage" => OutreachMapJob.Coverage,
        "places" => OutreachMapJob.Places,
        "areas" or "saved-areas" => OutreachMapJob.SavedAreas,
        "reports" => OutreachMapJob.Reports,
        "map-areas" => OutreachMapJob.MapAreas,
        "plan" => OutreachMapJob.Plan,
        _ => null,
    };

    public static string Slug(OutreachMapJob job) => job switch
    {
        OutreachMapJob.Places => "places",
        OutreachMapJob.SavedAreas => "saved-areas",
        OutreachMapJob.Reports => "reports",
        OutreachMapJob.MapAreas => "map-areas",
        OutreachMapJob.Plan => "plan",
        _ => "coverage",
    };

    public static string Href(OutreachMapJob job) => $"outreach/map?job={Slug(job)}";

    public static string PlanHref(string dayId) => $"outreach/map?day={Uri.EscapeDataString(dayId)}";

    public static string Name(OutreachLayer layer) => layer switch
    {
        OutreachLayer.Coverage => "Coverage",
        OutreachLayer.SkippedPlaces => "Places outreach skips",
        OutreachLayer.Marks => "No junk mail and hostile",
        OutreachLayer.SavedAreas => "Saved areas",
        _ => "Walkers' reports",
    };
}
