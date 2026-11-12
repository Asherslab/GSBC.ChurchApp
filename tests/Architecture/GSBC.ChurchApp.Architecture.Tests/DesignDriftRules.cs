using System.Text.Json;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class DesignDriftRules
{
    private const string SharedComponents = "src/Platform/GSBC.ChurchApp.Platform.Ui/";
    private const string AdminStylesheet = "src/Hosts/GSBC.ChurchApp.Hosts.Admin/wwwroot/css/admin.css";
    private const string RatchetFile = "tests/Architecture/GSBC.ChurchApp.Architecture.Tests/design-ratchets.json";

    private static readonly Dictionary<string, string> ClassesOnlyScriptsRead = new()
    {
        ["module-nav__area"] = "the sidebar's script finds an area to keep it open",
        ["segmented__option"] = "the segmented control's script moves focus between options",
        ["swipe-pages__page"] = "the swipe script measures each page",
    };

    [Fact]
    public void EveryClassInMarkupIsDefinedInAStylesheet()
    {
        HashSet<string> defined = Stylesheets()
            .SelectMany(path => DefinedClass().Matches(Comment().Replace(File.ReadAllText(path), "")).Select(match => match.Groups[1].Value))
            .ToHashSet(StringComparer.Ordinal);

        List<string> undefined = Razor()
            .SelectMany(file => ClassesIn(file.Text).Select(name => (file.Path, Name: name)))
            .Where(use => !defined.Contains(use.Name) && !ClassesOnlyScriptsRead.ContainsKey(use.Name))
            .Select(use => $"{use.Path}: .{use.Name}")
            .Distinct()
            .ToList();

        Assert.True(undefined.Count == 0, string.Join(Environment.NewLine, undefined));
    }

    [Fact]
    public void SpacingComesFromTokens() =>
        AssertRatchet("spacing", Stylesheets().Where(path => Path.GetFileName(path) != "tokens.css"), text =>
            SpacingDeclaration().Matches(Comment().Replace(text, "")).Sum(match => LiteralLength().Count(match.Groups[1].Value + match.Groups[2].Value)));

    [Fact]
    public void ButtonsComeFromTheSharedButton() =>
        AssertRatchet("buttons", Razor().Where(file => !file.Path.StartsWith(SharedComponents, StringComparison.Ordinal)).Select(file => Full(file.Path)),
            text => RawButton().Count(text));

    [Fact]
    public void DatesAndTimesUseTheSharedFields() =>
        AssertRatchet("nativeDates", Razor().Where(file => !file.Path.StartsWith(SharedComponents, StringComparison.Ordinal)).Select(file => Full(file.Path)),
            text => NativeDateInput().Count(text));

    [Fact]
    public void DialogsPutTheirMainActionFirst() =>
        AssertRatchet("quietFirst", Razor().Select(file => Full(file.Path)), text =>
            SheetBlock().Matches(text).SelectMany(sheet => ActionsBlock().Matches(sheet.Value))
                .Count(block => block.Groups[1].Value.Split('{', '}').Any(branch => QuietBeforeMain(ButtonTag().Matches(branch).Select(tag => tag.Value).ToList()))));

    [Fact]
    public void ListsAndRecordsUseTheWorkbench() =>
        AssertRatchet("splitView", Razor().Where(file => !file.Path.StartsWith(SharedComponents, StringComparison.Ordinal)).Select(file => Full(file.Path)),
            text => SplitViewTag().Count(text));

    [Fact]
    public void DeleteIsAnActionOnTheItem() =>
        AssertRatchet("deleteOutsideMenu", Razor().Where(file => !file.Path.StartsWith(SharedComponents, StringComparison.Ordinal)).Select(file => Full(file.Path)),
            text => DeleteInFoot().Count(text) + DangerButton().Count(ActionsBlock().Replace(text, "")));

    [Fact]
    public void TheSidebarKeepsItsWidthWhenAScrollbarAppears()
    {
        string admin = File.ReadAllText(Full(AdminStylesheet));

        Assert.Contains("scrollbar-gutter: stable", Rule(admin, ".admin-sidebar"), StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: normal", Rule(admin, ".module-nav__title"), StringComparison.Ordinal);
    }

    private static bool QuietBeforeMain(List<string> buttons)
    {
        int firstQuiet = buttons.FindIndex(button => button.Contains("ButtonVariant.Quiet", StringComparison.Ordinal));
        return firstQuiet >= 0 && buttons.Skip(firstQuiet).Any(button => !button.Contains("ButtonVariant.Quiet", StringComparison.Ordinal));
    }

    private static string Rule(string css, string selector)
    {
        int start = css.IndexOf($"\n{selector} {{", StringComparison.Ordinal);
        Assert.True(start >= 0, $"{selector} has no rule of its own.");
        return css[start..css.IndexOf('}', start)];
    }

    private static void AssertRatchet(string kind, IEnumerable<string> files, Func<string, int> count)
    {
        Dictionary<string, int> allowed = Ratchets()[kind];
        Dictionary<string, int> today = files
            .Select(path => (Path: Relative(path), Count: count(File.ReadAllText(path))))
            .Where(file => file.Count > 0)
            .ToDictionary(file => file.Path, file => file.Count);

        List<string> problems =
        [
            .. today.Where(file => file.Value > allowed.GetValueOrDefault(file.Key))
                .Select(file => $"{file.Key}: {file.Value}, more than the {allowed.GetValueOrDefault(file.Key)} allowed"),
            .. allowed.Where(file => today.GetValueOrDefault(file.Key) < file.Value)
                .Select(file => $"{file.Key}: down to {today.GetValueOrDefault(file.Key)}; lower its {kind} entry in {RatchetFile}"),
        ];

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    private static Dictionary<string, Dictionary<string, int>> Ratchets() =>
        JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(File.ReadAllText(Full(RatchetFile)))
        ?? throw new InvalidOperationException($"{RatchetFile} is empty.");

    private static IEnumerable<string> ClassesIn(string razor) =>
        ClassAttribute().Matches(razor)
            .Select(match => AttributeValue(razor, match.Index + match.Length))
            .SelectMany(value => RazorExpression().Replace(value, " ").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(name => PlainClass().IsMatch(name));

    private static string AttributeValue(string razor, int start)
    {
        int depth = 0;
        for (int at = start; at < razor.Length; at++)
        {
            switch (razor[at])
            {
                case '(' when depth > 0 || (at > 0 && razor[at - 1] == '@'):
                    depth++;
                    break;
                case ')' when depth > 0:
                    depth--;
                    break;
                case '"' when depth == 0:
                    return razor[start..at];
            }
        }
        return "";
    }

    private static IEnumerable<string> Stylesheets() =>
        RepoFiles.Under("src", ".css").Where(path => !Relative(path).Contains("/shell/", StringComparison.Ordinal));

    private static IEnumerable<(string Path, string Text)> Razor() =>
        RepoFiles.Under("src", ".razor").Select(path => (Path: Relative(path), Text: File.ReadAllText(path)));

    private static string Relative(string path) => Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/');

    private static string Full(string relative) => Path.Combine(RepoRoot.Path, relative.Replace('/', Path.DirectorySeparatorChar));

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comment();

    [GeneratedRegex(@"\.(-?[_a-zA-Z][\w-]*)")]
    private static partial Regex DefinedClass();

    [GeneratedRegex(@"\b[Cc]lass=""")]
    private static partial Regex ClassAttribute();

    [GeneratedRegex(@"@\((?>[^()]+|\((?<depth>)|\)(?<-depth>))*\)(?(depth)(?!))|@[A-Za-z_][\w.]*")]
    private static partial Regex RazorExpression();

    [GeneratedRegex(@"^[a-z][a-z0-9]*(?:[-_]{1,2}[a-z0-9]+)*$")]
    private static partial Regex PlainClass();

    [GeneratedRegex(@"(?:^|[{;\s])(?:margin|padding)(?:-[a-z-]+)?\s*:\s*([^;}]+)|(?:^|[{;\s])(?:row-|column-)?gap\s*:\s*([^;}]+)", RegexOptions.Multiline)]
    private static partial Regex SpacingDeclaration();

    [GeneratedRegex(@"(?<![\w.-])[1-9]\d*(?:\.\d+)?px\b")]
    private static partial Regex LiteralLength();

    [GeneratedRegex(@"<Sheet\b.*?</Sheet>", RegexOptions.Singleline)]
    private static partial Regex SheetBlock();

    [GeneratedRegex(@"<Actions>(.*?)</Actions>", RegexOptions.Singleline)]
    private static partial Regex ActionsBlock();

    [GeneratedRegex(@"<Button\b[^>]*>")]
    private static partial Regex ButtonTag();

    [GeneratedRegex(@"<SplitView\b")]
    private static partial Regex SplitViewTag();

    [GeneratedRegex(@"editor-foot__delete")]
    private static partial Regex DeleteInFoot();

    [GeneratedRegex(@"<Button\b[^>]*ButtonVariant\.Danger")]
    private static partial Regex DangerButton();

    [GeneratedRegex(@"<button\b")]
    private static partial Regex RawButton();

    [GeneratedRegex(@"\b[Tt]ype=""(?:date|time|datetime-local)""")]
    private static partial Regex NativeDateInput();
}
