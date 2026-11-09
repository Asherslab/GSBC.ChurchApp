using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Admin.Components;

public static class DeletionWords
{
    private const string Gone = "It comes off every phone the next time it connects. This can't be undone.";

    public static IReadOnlyList<string> Speaker(SpeakerDetail speaker, SpeakerDetail? moveTo)
    {
        List<string> lines = [];
        if (speaker.Count == 0)
            lines.Add("No sermons are credited to them.");
        else
            lines.Add(moveTo is not null ? $"Their sermons move to {moveTo.Name}." : "Their sermons show without a speaker.");
        lines.Add($"Their photo goes with them. {Gone}");
        return lines;
    }

    public static IReadOnlyList<string> Series(SeriesDeletion deletion, SeriesDetail? moveTo)
    {
        List<string> lines = [];
        if (deletion.Sermons > 0)
            lines.Add(moveTo is not null ? $"Its sermons move to {moveTo.Name}." : "Its sermons won't be in a series.");
        if (deletion.Followers > 0)
            lines.Add(Followers(deletion.Followers) + (moveTo is null ? " stop hearing about its new sermons." : $" follow {moveTo.Name} instead."));
        lines.Add($"Its picture and description go with it. {Gone}");
        return lines;
    }

    private static string Followers(int count) => count == 1 ? "The one person or phone following it will" : $"The {count} people and phones following it will";
}
