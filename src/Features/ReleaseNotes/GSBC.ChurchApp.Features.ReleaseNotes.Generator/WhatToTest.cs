using System.Text;

namespace GSBC.ChurchApp.Features.ReleaseNotes.Generator;

public static class WhatToTest
{
    public const int AppleLimit = 4000;
    private const string NothingNew = "Nothing new to try in this build: it refreshes the app before the last one expires.";
    private const string AlsoHeading = "Also in this build:";
    private const string InTheApp = "What changed in each, and where, is under More → What's new in the app.";

    public static string Render(IEnumerable<Note> notes)
    {
        List<Note> ordered = BuildNotes.For(NoteAudience.Staff, notes).OrderByDescending(note => note.Highlight).ToList();
        if (ordered.Count == 0)
            return NothingNew;

        bool[] inFull = new bool[ordered.Count];
        int left = Layout(ordered, inFull).Left;
        for (int index = 0; index < ordered.Count; index++)
        {
            inFull[index] = true;
            (string text, int leftOut) = Layout(ordered, inFull);
            if (text.Length > AppleLimit || leftOut > left)
                inFull[index] = false;
        }
        return Layout(ordered, inFull).Text;
    }

    private static (string Text, int Left) Layout(List<Note> ordered, bool[] inFull)
    {
        List<string> blocks = ordered.Where((_, index) => inFull[index]).Select(Entry).ToList();
        List<string> named = ordered.Where((_, index) => !inFull[index]).Select(note => $"• {Heading(note)}").ToList();

        StringBuilder text = new(string.Join("\n\n", blocks));
        if (named.Count > 0)
        {
            text.Append(text.Length == 0 ? "" : "\n\n").Append(AlsoHeading);
            for (int index = 0; index < named.Count; index++)
            {
                string rest = $"\n…and {named.Count - index} more under More → What's new in the app.";
                bool last = index == named.Count - 1;
                if (text.Length + 1 + named[index].Length + (last ? 0 : rest.Length) + 2 + InTheApp.Length > AppleLimit)
                {
                    text.Append(rest);
                    return (text.ToString(), named.Count - index);
                }
                text.Append('\n').Append(named[index]);
            }
        }
        return (text.Append("\n\n").Append(InTheApp).ToString(), 0);
    }

    private static string Heading(Note note) => $"{note.Title}{(note.Audience == NoteAudience.Staff ? " (staff only)" : "")}";

    private static string Entry(Note note)
    {
        List<string> lines = [Heading(note), note.Summary];
        if (note.Where is not null)
            lines.Add($"Where: {note.Where}");
        if (note.Try is not null)
            lines.Add($"Try: {note.Try}");
        return string.Join("\n", lines);
    }
}
