using System.Globalization;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class ThemeTokenRules
{
    private const double ReadableText = 4.5;

    private static readonly string[] Palettes = ["brand", "year-2026"];
    private static readonly string[] Schemes = ["light", "dark"];

    private static readonly (string Text, string Behind)[] Readable =
    [
        ("--ink", "--page"), ("--ink", "--surface"), ("--ink", "--sand"), ("--ink-2", "--surface"),
        ("--on-masthead", "--masthead"), ("--on-primary", "--primary"),
    ];

    private static readonly Lazy<IReadOnlyList<Rule>> Rules = new(ReadRules);

    public static TheoryData<string, string> EveryTheme()
    {
        TheoryData<string, string> data = [];
        foreach (string palette in Palettes)
            foreach (string scheme in Schemes)
                data.Add(palette, scheme);
        return data;
    }

    [Theory]
    [MemberData(nameof(EveryTheme))]
    public void APreviewShowsATheme(string palette, string scheme)
    {
        IReadOnlyDictionary<string, string> app = Computed(AppRoot(palette, scheme));
        List<string> violations = [];
        foreach (string page in Palettes)
            foreach (string pageScheme in Schemes)
            {
                Element root = AppRoot(page, pageScheme);
                Element frame = new(new Dictionary<string, string> { ["data-palette"] = palette, ["data-preview-scheme"] = scheme }, root);
                IReadOnlyDictionary<string, string> previewed = Computed(frame);
                violations.AddRange(app.Where(token => previewed.GetValueOrDefault(token.Key) != token.Value)
                    .Select(token => $"in a {page} {pageScheme} page: {token.Key} is {previewed.GetValueOrDefault(token.Key)}, the app has {token.Value}"));
            }

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(EveryTheme))]
    public void APreviewFollowingThePageShowsTheTheme(string palette, string scheme)
    {
        IReadOnlyDictionary<string, string> app = Computed(AppRoot(palette, scheme));
        List<string> violations = [];
        foreach (string page in Palettes)
        {
            Element frame = new(new Dictionary<string, string> { ["data-palette"] = palette }, AppRoot(page, scheme));
            IReadOnlyDictionary<string, string> previewed = Computed(frame);
            violations.AddRange(app.Where(token => previewed.GetValueOrDefault(token.Key) != token.Value)
                .Select(token => $"in a {page} page: {token.Key} is {previewed.GetValueOrDefault(token.Key)}, the app has {token.Value}"));
        }

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(EveryTheme))]
    public void ThemeTextIsReadable(string palette, string scheme)
    {
        IReadOnlyDictionary<string, string> tokens = Computed(AppRoot(palette, scheme));
        List<string> violations = Readable
            .Select(pair => (pair.Text, pair.Behind, Contrast: Contrast(tokens[pair.Text], tokens[pair.Behind])))
            .Where(pair => pair.Contrast < ReadableText)
            .Select(pair => string.Create(CultureInfo.InvariantCulture, $"{pair.Text} on {pair.Behind}: {pair.Contrast:0.0}:1"))
            .ToList();

        Assert.Empty(violations);
    }

    [Theory]
    [MemberData(nameof(Built))]
    public void LightAndDarkDiffer(string palette)
    {
        IReadOnlyDictionary<string, string> light = Computed(AppRoot(palette, "light"));
        IReadOnlyDictionary<string, string> dark = Computed(AppRoot(palette, "dark"));

        Assert.True(Luminance(light["--page"]) > Luminance(dark["--page"]));
        Assert.True(Luminance(light["--surface"]) > Luminance(dark["--surface"]));
        Assert.True(Luminance(light["--ink"]) < Luminance(dark["--ink"]));
    }

    public static TheoryData<string> Built() => [.. Palettes];

    private static Element AppRoot(string palette, string scheme) =>
        new(new Dictionary<string, string> { ["data-theme"] = palette, ["data-scheme"] = scheme }, null);

    private sealed record Element(IReadOnlyDictionary<string, string> Attributes, Element? Parent)
    {
        public bool IsRoot => Parent is null;
    }

    private sealed record Rule(IReadOnlyList<Compound> Selector, int Specificity, int Order, IReadOnlyDictionary<string, string> Tokens);

    private sealed record Compound(bool Root, IReadOnlyList<(string Name, string? Value, bool Not)> Attributes)
    {
        public bool Matches(Element element) =>
            (!Root || element.IsRoot)
            && Attributes.All(attribute => attribute.Not != Has(element, attribute.Name, attribute.Value));

        private static bool Has(Element element, string name, string? value) =>
            element.Attributes.TryGetValue(name, out string? actual) && (value is null || actual == value);
    }

    private static IReadOnlyDictionary<string, string> Computed(Element element)
    {
        Dictionary<string, string> inherited = element.Parent is { } parent ? new(Computed(parent)) : [];
        Dictionary<string, string> declared = Rules.Value
            .Where(rule => Matches(rule.Selector, element))
            .OrderBy(rule => rule.Specificity).ThenBy(rule => rule.Order)
            .SelectMany(rule => rule.Tokens)
            .GroupBy(token => token.Key)
            .ToDictionary(group => group.Key, group => group.Last().Value);
        foreach ((string name, string value) in declared)
            inherited[name] = Resolve(value, declared, inherited);
        return inherited;
    }

    private static string Resolve(string value, Dictionary<string, string> declared, Dictionary<string, string> inherited) =>
        VarReference().Replace(value, match =>
        {
            string name = match.Groups["name"].Value;
            return declared.TryGetValue(name, out string? own) ? Resolve(own, declared, inherited)
                : inherited.GetValueOrDefault(name, match.Groups["fallback"].Value.Trim());
        });

    private static bool Matches(IReadOnlyList<Compound> selector, Element element)
    {
        if (!selector[^1].Matches(element))
            return false;
        Element? ancestor = element.Parent;
        for (int index = selector.Count - 2; index >= 0; index--)
        {
            while (ancestor is not null && !selector[index].Matches(ancestor))
                ancestor = ancestor.Parent;
            if (ancestor is null)
                return false;
            ancestor = ancestor.Parent;
        }
        return true;
    }

    private static IReadOnlyList<Rule> ReadRules()
    {
        string css = Comment().Replace(File.ReadAllText(Path.Combine(RepoRoot.Path, "src/Platform/GSBC.ChurchApp.Platform.Ui/wwwroot/css/tokens.css")), "");
        List<Rule> rules = [];
        foreach (Match block in Block().Matches(css))
        {
            string selectors = block.Groups["selectors"].Value.Trim();
            if (selectors.StartsWith('@'))
                continue;
            Dictionary<string, string> tokens = Declaration().Matches(block.Groups["body"].Value)
                .ToDictionary(declaration => declaration.Groups["name"].Value, declaration => declaration.Groups["value"].Value.Trim());
            foreach (string selector in selectors.Split(',', StringSplitOptions.TrimEntries))
            {
                List<Compound> compounds = selector.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(ParseCompound).ToList();
                int specificity = compounds.Sum(compound => (compound.Root ? 1 : 0) + compound.Attributes.Count);
                rules.Add(new Rule(compounds, specificity, rules.Count, tokens));
            }
        }
        return rules;
    }

    private static Compound ParseCompound(string text)
    {
        List<(string, string?, bool)> attributes = Attribute().Matches(text)
            .Select(match => (match.Groups["name"].Value, match.Groups["value"].Success ? match.Groups["value"].Value : null, match.Groups["not"].Success))
            .ToList();
        string rest = Attribute().Replace(text, "");
        return rest is "" or ":root" ? new Compound(rest == ":root", attributes) : throw new FormatException($"tokens.css uses a selector this rule can't read: {text}");
    }

    private static double Contrast(string first, string second)
    {
        double one = Luminance(first);
        double two = Luminance(second);
        return (Math.Max(one, two) + 0.05) / (Math.Min(one, two) + 0.05);
    }

    private static double Luminance(string hex)
    {
        Assert.Matches(Hex(), hex);
        double Channel(int at)
        {
            double value = int.Parse(hex.AsSpan(at, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return value <= 0.03928 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(1) + 0.7152 * Channel(3) + 0.0722 * Channel(5);
    }

    [GeneratedRegex(@"/\*.*?\*/", RegexOptions.Singleline)]
    private static partial Regex Comment();

    [GeneratedRegex(@"(?<selectors>[^{}]+)\{(?<body>[^{}]*)\}")]
    private static partial Regex Block();

    [GeneratedRegex(@"(?<name>--[\w-]+)\s*:\s*(?<value>[^;]+);")]
    private static partial Regex Declaration();

    [GeneratedRegex(@"(?<not>:not\()?\[(?<name>[\w-]+)(?:=""(?<value>[^""]*)"")?\]\)?")]
    private static partial Regex Attribute();

    [GeneratedRegex(@"var\((?<name>--[\w-]+)\s*(?:,(?<fallback>[^)]*))?\)")]
    private static partial Regex VarReference();

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex Hex();
}
