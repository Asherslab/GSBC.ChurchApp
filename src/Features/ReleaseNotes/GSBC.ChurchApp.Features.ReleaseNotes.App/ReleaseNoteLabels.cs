namespace GSBC.ChurchApp.Features.ReleaseNotes.App;

internal static class ReleaseNoteLabels
{
    public static string NewIn(ReleaseNote note, bool forStaff, string? appVersion) =>
        $"New in {VersionName(note.Version, forStaff, appVersion)}";

    public static string Heading(string? version, bool forStaff, string? appVersion) =>
        version is null && forStaff ? "In this test build" : $"Version {version ?? appVersion ?? "next"}";

    private static string VersionName(string? version, bool forStaff, string? appVersion) =>
        version ?? (forStaff ? "this test build" : appVersion ?? "this update");
}
