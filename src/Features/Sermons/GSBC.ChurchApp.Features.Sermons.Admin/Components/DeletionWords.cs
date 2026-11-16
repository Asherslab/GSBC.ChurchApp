using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Admin.Components;

public static class DeletionWords
{
    private static string Gone(string what) => $"The {what} comes off each phone the next time that phone connects. This can't be undone.";

    public static IReadOnlyList<string> Speaker(SpeakerDetail speaker, SpeakerDetail? moveTo)
    {
        List<string> lines = [];
        if (speaker.Count == 0)
            lines.Add("No sermons name this speaker.");
        else
            lines.Add(moveTo is not null ? $"The speaker's sermons move to {moveTo.Name}." : "The speaker's sermons will show without a speaker.");
        lines.Add($"The speaker's photo is deleted too. {Gone("speaker")}");
        return lines;
    }

    public static IReadOnlyList<string> Series(SeriesDeletion deletion, SeriesDetail? moveTo)
    {
        List<string> lines = [];
        if (deletion.Sermons > 0)
            lines.Add(moveTo is not null ? $"The series' sermons move to {moveTo.Name}." : "The series' sermons will be left without a series.");
        if (deletion.Followers > 0)
            lines.Add(Followers(deletion.Followers) + (moveTo is null ? " stop hearing about its new sermons." : $" follow {moveTo.Name} instead."));
        lines.Add($"The series' picture and description are deleted too. {Gone("series")}");
        return lines;
    }

    private static string Followers(int count) => count == 1 ? "The one person or phone following this series will" : $"The {count} people and phones following this series will";
}
