using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GSBC.ChurchApp.Features.ReleaseNotes.Generator;

public static class WhatToTest
{
    public const int AppleLimit = 4000;
    private const string NothingNew = "Nothing new to try in this build: it refreshes the app before the last one expires.";

    public static string Render(IEnumerable<Note> notes)
    {
        List<string> entries = BuildNotes.For(NoteAudience.Staff, notes).Select(Entry).ToList();
        if (entries.Count == 0)
            return NothingNew;

        StringBuilder text = new();
        for (int index = 0; index < entries.Count; index++)
        {
            string rest = $"…and {entries.Count - index} more under More → What's new in the app.";
            string separator = text.Length == 0 ? "" : "\n\n";
            bool fitsWithRoomForRest = text.Length + separator.Length + entries[index].Length + (index == entries.Count - 1 ? 0 : 2 + rest.Length) <= AppleLimit;
            if (!fitsWithRoomForRest)
            {
                text.Append(separator).Append(rest);
                break;
            }
            text.Append(separator).Append(entries[index]);
        }
        return text.ToString();
    }

    private static string Entry(Note note)
    {
        List<string> lines = [$"{note.Title}{(note.Audience == NoteAudience.Staff ? " (staff only)" : "")}", note.Summary];
        if (note.Changed is not null)
            lines.Add(note.Changed);
        if (note.Where is not null)
            lines.Add($"Where: {note.Where}");
        if (note.Try is not null)
            lines.Add($"Try: {note.Try}");
        return string.Join("\n", lines);
    }
}
