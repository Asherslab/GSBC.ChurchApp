using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.ReleaseNotes.Generator;

public static class NoteFile
{
    public const string UnreleasedFolder = "unreleased";
    public const int LongestSummary = 160;
    private const string StaffHeading = "## For staff";

    private static readonly Regex VersionFolder = new(@"^\d+\.\d+(\.\d+)?$");
    private static readonly Regex Slug = new(@"^[a-z0-9]+(-[a-z0-9]+)*$");
    private static readonly string[] StaffLabels = ["Changed", "Where", "Try"];

    public static Note Parse(string folder, string fileName, string text)
    {
        string id = fileName.EndsWith(".md", StringComparison.Ordinal) ? fileName.Substring(0, fileName.Length - 3) : fileName;
        if (!Slug.IsMatch(id))
            throw new NoteException($"name the file in lower-case words joined by hyphens, like sign-in-with-a-passkey.md (not \"{fileName}\")");
        if (folder != UnreleasedFolder && !VersionFolder.IsMatch(folder))
            throw new NoteException($"a note lives in changelog/{UnreleasedFolder}/ or a version folder like changelog/1.1/ (not \"{folder}\")");

        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0 || lines[0].Trim() != "---")
            throw new NoteException("start with front matter between two --- lines");
        int end = Array.FindIndex(lines, 1, line => line.Trim() == "---");
        if (end < 0)
            throw new NoteException("close the front matter with a --- line");

        Dictionary<string, string> front = FrontMatter(lines.Skip(1).Take(end - 1));
        string[] body = lines.Skip(end + 1).ToArray();
        int staffStart = Array.FindIndex(body, line => line.Trim() == StaffHeading);
        string summary = Paragraph(staffStart < 0 ? body : body.Take(staffStart));
        Dictionary<string, string> staff = staffStart < 0 ? [] : StaffSection(body.Skip(staffStart + 1));

        if (summary.Length == 0)
            throw new NoteException("write the one-line summary for everyone under the front matter");
        if (summary.Length > LongestSummary)
            throw new NoteException($"keep the summary to one short line ({summary.Length} characters; at most {LongestSummary})");

        return new Note(
            id,
            folder == UnreleasedFolder ? null : folder,
            Required(front, "title"),
            Audience(Required(front, "audience")),
            Flag(front, "highlight"),
            Optional(front, "icon"),
            Optional(front, "link")?.TrimStart('/'),
            Optional(front, "image"),
            Optional(front, "area"),
            summary,
            Optional(staff, "Changed"),
            Optional(staff, "Where"),
            Optional(staff, "Try"));
    }

    private static Dictionary<string, string> FrontMatter(IEnumerable<string> lines)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (string line in lines.Where(line => line.Trim().Length > 0))
        {
            int colon = line.IndexOf(':');
            if (colon <= 0)
                throw new NoteException($"front matter lines are \"key: value\" (not \"{line.Trim()}\")");
            string key = line.Substring(0, colon).Trim();
            if (!Known.Contains(key))
                throw new NoteException($"unknown front matter \"{key}\"; use {string.Join(", ", Known)}");
            values[key] = line.Substring(colon + 1).Trim();
        }
        return values;
    }

    private static readonly string[] Known = ["title", "audience", "highlight", "icon", "link", "image", "area"];

    private static Dictionary<string, string> StaffSection(IEnumerable<string> lines)
    {
        Dictionary<string, List<string>> parts = new(StringComparer.Ordinal);
        List<string>? current = null;
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0)
                continue;
            string? label = StaffLabels.FirstOrDefault(name => line.StartsWith(name + ":", StringComparison.Ordinal));
            if (label is not null)
            {
                current = parts[label] = [line.Substring(label.Length + 1).Trim()];
                continue;
            }
            if (current is null)
                throw new NoteException($"under \"{StaffHeading}\", start each part with Changed:, Where: or Try: (not \"{line}\")");
            current.Add(line);
        }
        return parts.ToDictionary(part => part.Key, part => string.Join(" ", part.Value.Where(text => text.Length > 0)));
    }

    private static string Paragraph(IEnumerable<string> lines) =>
        string.Join(" ", lines.Select(line => line.Trim()).Where(line => line.Length > 0));

    private static string Required(Dictionary<string, string> values, string key) =>
        Optional(values, key) ?? throw new NoteException($"add \"{key}:\" to the front matter");

    private static string? Optional(Dictionary<string, string> values, string key) =>
        values.TryGetValue(key, out string? value) && value.Length > 0 ? value : null;

    private static bool Flag(Dictionary<string, string> values, string key) =>
        Optional(values, key) switch
        {
            null or "false" => false,
            "true" => true,
            { } other => throw new NoteException($"\"{key}\" is true or false (not \"{other}\")"),
        };

    private static NoteAudience Audience(string value) =>
        value switch
        {
            "everyone" => NoteAudience.Everyone,
            "staff" => NoteAudience.Staff,
            _ => throw new NoteException($"\"audience\" is everyone or staff (not \"{value}\")"),
        };
}

public sealed class NoteException(string message) : Exception(message);
