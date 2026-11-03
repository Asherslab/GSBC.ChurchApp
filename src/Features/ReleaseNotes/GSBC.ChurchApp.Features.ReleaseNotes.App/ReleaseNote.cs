namespace GSBC.ChurchApp.Features.ReleaseNotes.App;

public sealed record ReleaseNote(
    string Id,
    string? Version,
    string Title,
    string Summary,
    bool Highlight,
    bool StaffOnly,
    string? Icon,
    string? Link,
    string? Image,
    string? Area,
    string? Changed,
    string? Where,
    string? Try)
{
    public const string ImageFolder = "_content/GSBC.ChurchApp.Features.ReleaseNotes.App/release-notes/";

    public string? ImageUrl => Image is null ? null : ImageFolder + Image;
}
