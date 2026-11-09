using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Admin.Components;

public static class DeletionWords
{
    private const string Gone = "It comes off every phone the next time it connects. This can't be undone.";

    public static IReadOnlyList<string> Speaker(SpeakerDetail speaker, SpeakerDetail? moveTo, bool ours)
    {
        List<string> lines = [];
        if (speaker.Count == 0)
            lines.Add("No sermons are credited to them.");
        else if (moveTo is not null)
            lines.Add(ours ? $"Their sermons move to {moveTo.Name}." : $"Their sermons move to {moveTo.Name}, and so do later ones the website credits to “{speaker.Name}”.");
        else
            lines.Add(ours ? "Their sermons show without a speaker." : $"Their sermons show without a speaker. If the website credits a later sermon to “{speaker.Name}”, that adds them again as a new speaker.");
        lines.Add("Their photo goes with them. They come off every phone the next time it connects. This can't be undone.");
        return lines;
    }

    public static IReadOnlyList<string> Series(SeriesDetail series, SeriesDeletion deletion, SeriesDetail? moveTo)
    {
        List<string> lines = [];
        if (deletion.Sermons > 0 && moveTo is not null)
            lines.Add(series.FromWebsite ? $"Its sermons move to {moveTo.Name}, and so do later ones the website puts in this series." : $"Its sermons move to {moveTo.Name}.");
        else if (deletion.Sermons > 0)
            lines.Add(series.FromWebsite ? "Its sermons won't be in a series, and nor will later ones the website puts in it." : "Its sermons won't be in a series.");
        if (series.FromWebsite)
            lines.Add("It won't come back from the website.");
        if (deletion.Followers > 0)
            lines.Add(Followers(deletion.Followers) + (moveTo is null ? " stop hearing about its new sermons." : $" follow {moveTo.Name} instead."));
        lines.Add($"Its picture and description go with it. {Gone}");
        return lines;
    }

    private static string Followers(int count) => count == 1 ? "The one person or phone following it will" : $"The {count} people and phones following it will";
}
