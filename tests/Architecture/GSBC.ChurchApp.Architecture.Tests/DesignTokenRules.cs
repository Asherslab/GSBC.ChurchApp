using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class DesignTokenRules
{
    private const string Tokens = "tokens.css";
    private const string RootFallback = "html { font-size: 17px; }";

    [Fact]
    public void StylesheetsReadColoursFromTokens()
    {
        List<string> violations = Stylesheets()
            .Where(path => Path.GetFileName(path) != Tokens)
            .SelectMany(path => Lines(path).Where(line => LiteralColour().IsMatch(line.Text)).Select(line => $"{line.Where}: {line.Text}"))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void FontSizesFollowTheTextSizeSetting()
    {
        List<string> violations = Stylesheets()
            .SelectMany(path => Lines(path).Where(line => PixelFontSize().IsMatch(line.Text) && line.Text != RootFallback).Select(line => $"{line.Where}: {line.Text}"))
            .ToList();

        Assert.Empty(violations);
    }

    private static IEnumerable<string> Stylesheets() =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot.Path, "src"), "*.css", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}shell{Path.DirectorySeparatorChar}"));

    private static IEnumerable<(string Where, string Text)> Lines(string path) =>
        File.ReadLines(path).Select((text, index) => ($"{Path.GetRelativePath(RepoRoot.Path, path)}:{index + 1}", text.Trim()));

    [GeneratedRegex(@"#[0-9a-fA-F]{3,8}\b|\b(rgb|hsl)a?\(\s*\d")]
    private static partial Regex LiteralColour();

    [GeneratedRegex(@"font-size:\s*[\d.]+px")]
    private static partial Regex PixelFontSize();
}
