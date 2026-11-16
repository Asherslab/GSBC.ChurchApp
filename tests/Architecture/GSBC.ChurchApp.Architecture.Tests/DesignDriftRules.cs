using System.Text.Json;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class DesignDriftRules
{
    private const string SharedComponents = "src/Platform/GSBC.ChurchApp.Platform.Ui/";
    private const string AdminStylesheet = "src/Hosts/GSBC.ChurchApp.Hosts.Admin/wwwroot/css/admin.css";
    private const string RatchetFile = "tests/Architecture/GSBC.ChurchApp.Architecture.Tests/design-ratchets.json";

    private const string Pair = "ui-pair";

    private static readonly HashSet<string> FieldComponents =
        ["TextField", "DateField", "TimeField", "DateTimeField", "SelectField", "SearchPicker", "CodeField", "ServicePicker"];

    private static readonly HashSet<string> VoidElements = ["input", "img", "br", "hr", "meta", "link", "source", "col", "area", "wbr"];

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
    public void FieldsSideBySideUseThePair()
    {
        HashSet<string> sideBySide = ClassesLaidOutSideBySide();

        AssertRatchet("fieldRows", Razor().Select(file => Full(file.Path)), text =>
        {
            string markup = text.Split("@code", 2)[0];
            List<Tag> tags = Tags(markup).ToList();
            return tags.Select((tag, index) => (tag, index)).Count(open => open.tag is { Closing: false, SelfClosing: false } && char.IsLower(open.tag.Name[0])
                && ClassesOf(open.tag.Text) is var classes && !classes.Contains(Pair) && classes.Any(sideBySide.Contains)
                && FieldsIn(markup, tags, open.index) >= 2);
        });
    }

    [Fact]
    public void FocusNeverReshapesWhatItRings()
    {
        List<string> reshaping = Stylesheets()
            .SelectMany(path => CssRule().Matches(Comment().Replace(File.ReadAllText(path), ""))
                .Where(rule => rule.Groups[2].Value.Contains("border-radius", StringComparison.Ordinal))
                .SelectMany(rule => rule.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries))
                .Where(selector => selector.StartsWith(":focus", StringComparison.Ordinal))
                .Select(selector => $"{Relative(path)}: {selector} changes the shape of whatever has focus; wrap it in :where() so a component's own radius wins"))
            .ToList();

        Assert.True(reshaping.Count == 0, string.Join(Environment.NewLine, reshaping));
    }

    private static HashSet<string> ClassesLaidOutSideBySide() =>
        Stylesheets()
            .SelectMany(path => CssRule().Matches(Comment().Replace(File.ReadAllText(path), "")))
            .Where(rule => SideBySide(rule.Groups[2].Value))
            .SelectMany(rule => rule.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries))
            .Select(selector => DefinedClass().Matches(LastCompound().Match(selector).Value).Select(match => match.Groups[1].Value))
            .SelectMany(classes => classes)
            .Where(name => !name.StartsWith(Pair, StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

    private static bool SideBySide(string declarations) =>
        (ColumnTracks().Match(declarations) is { Success: true } columns && Tracks(columns.Groups[1].Value) > 1)
        || (FlexDisplay().IsMatch(declarations) && !ColumnDirection().IsMatch(declarations));

    private static int Tracks(string template)
    {
        if (RepeatCount().Match(template) is { Success: true } repeat)
            return int.TryParse(repeat.Groups[1].Value, out int count) ? count : 2;
        int depth = 0, tracks = 0;
        bool inTrack = false;
        foreach (char at in template.Trim())
        {
            depth += at switch { '(' => 1, ')' => -1, _ => 0 };
            bool gap = depth == 0 && char.IsWhiteSpace(at);
            if (!gap && !inTrack)
                tracks++;
            inTrack = !gap;
        }
        return tracks;
    }

    private static HashSet<string> ClassesOf(string tag) =>
        ClassAttribute().Match(tag) is { Success: true } attribute
            ? AttributeValue(tag, attribute.Index + attribute.Length).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.Ordinal)
            : [];

    private static int FieldsIn(string razor, List<Tag> tags, int index)
    {
        int depth = 0, fields = 0, previousEnd = tags[index].At + tags[index].Text.Length;
        (Tag Tag, bool Repeated)? wrapper = null;
        foreach (Tag tag in tags.Skip(index + 1))
        {
            bool repeated = Repeated().IsMatch(razor[previousEnd..tag.At]);
            previousEnd = tag.At + tag.Text.Length;
            if (depth == 0 && tag.Closing)
                break;
            if (depth == 1 && wrapper is { } holder && !tag.Closing)
            {
                fields += FieldComponents.Contains(tag.Name) ? holder.Repeated ? 2 : 1 : 0;
                wrapper = null;
            }
            if (depth == 0 && !tag.Closing && FieldComponents.Contains(tag.Name))
                fields += repeated ? 2 : 1;
            else if (depth == 0 && tag is { Closing: false, SelfClosing: false } && char.IsLower(tag.Name[0]) && !ClassesOf(tag.Text).Contains(Pair))
                wrapper = (tag, repeated);
            depth += tag.Closing ? -1 : tag.SelfClosing ? 0 : 1;
        }
        return fields;
    }

    private static IEnumerable<Tag> Tags(string razor)
    {
        for (int at = razor.IndexOf('<'); at >= 0 && at < razor.Length - 1; at = razor.IndexOf('<', at + 1))
        {
            bool closing = razor[at + 1] == '/';
            int nameStart = closing ? at + 2 : at + 1;
            if (nameStart >= razor.Length || !char.IsLetter(razor[nameStart]))
                continue;
            int nameEnd = nameStart;
            while (nameEnd < razor.Length && (char.IsLetterOrDigit(razor[nameEnd]) || razor[nameEnd] is '.' or '-'))
                nameEnd++;
            string name = razor[nameStart..nameEnd];
            int end = nameEnd;
            for (bool quoted = false; end < razor.Length && (quoted || razor[end] != '>'); end++)
            {
                if (razor[end] == '"')
                    quoted = !quoted;
            }
            string text = razor[at..Math.Min(end + 1, razor.Length)];
            yield return new Tag(at, name, text, closing, !closing && (text.EndsWith("/>", StringComparison.Ordinal) || VoidElements.Contains(name)));
            at = end;
        }
    }

    private sealed record Tag(int At, string Name, string Text, bool Closing, bool SelfClosing);

    [Fact]
    public void TheSidebarKeepsItsWidthWhenAScrollbarAppears()
    {
        string admin = File.ReadAllText(Full(AdminStylesheet));

        Assert.Contains("scrollbar-gutter: stable", Rule(admin, ".admin-sidebar"), StringComparison.Ordinal);
        Assert.Contains("overflow-wrap: normal", Rule(admin, ".module-nav__title"), StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyAComposerRidesTheKeyboard()
    {
        string ui = Comment().Replace(File.ReadAllText(Full(SharedComponents + "wwwroot/css/ui.css")), "");
        HashSet<string> staysDown = DefinedClass().Matches(KeyboardStaysDown().Match(ui).Groups[1].Value)
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        List<string> riding = Stylesheets()
            .SelectMany(path => CssRule().Matches(Comment().Replace(File.ReadAllText(path), ""))
                .Where(rule => StickyPosition().IsMatch(rule.Groups[2].Value) && BottomOffset().IsMatch(rule.Groups[2].Value))
                .SelectMany(rule => rule.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .Select(selector => (Path: Relative(path), Selector: selector, Docked: DockedClass(selector))))
            .Where(rule => rule.Docked is null || !staysDown.Contains(rule.Docked))
            .Select(rule => $"{rule.Path}: {rule.Selector} is docked at the foot; give it a class and add that to ui.css's `.keyboard-open :is(…) {{ position: static; }}`")
            .ToList();

        Assert.True(riding.Count == 0, string.Join(Environment.NewLine, riding));
    }

    private static string? DockedClass(string selector) =>
        DefinedClass().Matches(selector).LastOrDefault()?.Groups[1].Value.Split("--")[0];

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

    [GeneratedRegex("^[a-z][a-z0-9]*(?:[-_]{1,2}[a-z0-9]+)*$")]
    private static partial Regex PlainClass();

    [GeneratedRegex(@"(?:^|[{;\s])(?:margin|padding)(?:-[a-z-]+)?\s*:\s*([^;}]+)|(?:^|[{;\s])(?:row-|column-)?gap\s*:\s*([^;}]+)", RegexOptions.Multiline)]
    private static partial Regex SpacingDeclaration();

    [GeneratedRegex(@"(?<![\w.-])[1-9]\d*(?:\.\d+)?px\b")]
    private static partial Regex LiteralLength();

    [GeneratedRegex(@"\.keyboard-open :is\(([^)]*)\) \{ position: static; \}")]
    private static partial Regex KeyboardStaysDown();

    [GeneratedRegex(@"([^{}]+)\{([^{}]*)\}")]
    private static partial Regex CssRule();

    [GeneratedRegex(@"position\s*:\s*sticky")]
    private static partial Regex StickyPosition();

    [GeneratedRegex(@"(?:^|[;\s])bottom\s*:")]
    private static partial Regex BottomOffset();

    [GeneratedRegex(@"(?:^|[\s>+~])([^\s>+~]+)\s*$")]
    private static partial Regex LastCompound();

    [GeneratedRegex(@"grid-template-columns\s*:\s*([^;]+)")]
    private static partial Regex ColumnTracks();

    [GeneratedRegex(@"^\s*repeat\(\s*([\w-]+)\s*,")]
    private static partial Regex RepeatCount();

    [GeneratedRegex(@"(?:^|[;\s])display\s*:\s*(?:inline-)?flex\b")]
    private static partial Regex FlexDisplay();

    [GeneratedRegex(@"flex-direction\s*:\s*column")]
    private static partial Regex ColumnDirection();

    [GeneratedRegex(@"@(?:foreach|for)\s*\(")]
    private static partial Regex Repeated();

    [GeneratedRegex(@"<Sheet\b.*?</Sheet>", RegexOptions.Singleline)]
    private static partial Regex SheetBlock();

    [GeneratedRegex("<Actions>(.*?)</Actions>", RegexOptions.Singleline)]
    private static partial Regex ActionsBlock();

    [GeneratedRegex(@"<Button\b[^>]*>")]
    private static partial Regex ButtonTag();

    [GeneratedRegex(@"<SplitView\b")]
    private static partial Regex SplitViewTag();

    [GeneratedRegex("editor-foot__delete")]
    private static partial Regex DeleteInFoot();

    [GeneratedRegex(@"<Button\b[^>]*ButtonVariant\.Danger")]
    private static partial Regex DangerButton();

    [GeneratedRegex(@"<button\b")]
    private static partial Regex RawButton();

    [GeneratedRegex(@"\b[Tt]ype=""(?:date|time|datetime-local)""")]
    private static partial Regex NativeDateInput();
}
