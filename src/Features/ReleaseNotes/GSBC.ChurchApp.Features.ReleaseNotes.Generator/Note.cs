namespace GSBC.ChurchApp.Features.ReleaseNotes.Generator;

public enum NoteAudience
{
    Everyone,
    Staff,
}

public sealed class Note(
    string id, string? version, string title, NoteAudience audience, string added, bool highlight, string? icon, string? link,
    string? image, string? area, string summary, string? changed, string? where, string? @try)
{
    public string Id { get; } = id;
    public string? Version { get; } = version;
    public string Title { get; } = title;
    public NoteAudience Audience { get; } = audience;
    public string Added { get; } = added;
    public bool Highlight { get; } = highlight;
    public string? Icon { get; } = icon;
    public string? Link { get; } = link;
    public string? Image { get; } = image;
    public string? Area { get; } = area;
    public string Summary { get; } = summary;
    public string? Changed { get; } = changed;
    public string? Where { get; } = where;
    public string? Try { get; } = @try;

    public Note ForEveryone() => new(Id, Version, Title, Audience, Added, Highlight, Icon, Link, Image, Area, Summary, null, null, null);
}
