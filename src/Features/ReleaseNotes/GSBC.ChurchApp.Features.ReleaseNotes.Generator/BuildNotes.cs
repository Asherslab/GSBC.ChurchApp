namespace GSBC.ChurchApp.Features.ReleaseNotes.Generator;

public static class BuildNotes
{
    public static IReadOnlyList<Note> For(NoteAudience build, IEnumerable<Note> notes) =>
        notes
            .Where(note => build == NoteAudience.Staff || note.Audience == NoteAudience.Everyone)
            .Select(note => build == NoteAudience.Staff ? note : note.ForEveryone())
            .OrderByDescending(note => VersionKey(note.Version))
            .ThenByDescending(note => note.Highlight)
            .ThenBy(note => note.Title, StringComparer.Ordinal)
            .ToList();

    public static NoteAudience AudienceOf(string? property) =>
        property?.Trim().ToLowerInvariant() switch
        {
            "staff" => NoteAudience.Staff,
            "everyone" or "" or null => NoteAudience.Everyone,
            { } other => throw new NoteException($"GsbcReleaseNotes is staff or everyone (not \"{other}\")"),
        };

    private static Version VersionKey(string? version) =>
        version is null ? new Version(int.MaxValue, 0) : Version.Parse(version);
}
