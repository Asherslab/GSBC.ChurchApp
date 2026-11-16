using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class WordingRules
{
    private static readonly Dictionary<string, string> WaitingForTheirStation = new();

    [Fact]
    public void WordsOnScreenFollowTheWordingRules()
    {
        List<string> broken = Shipped()
            .Where(file => !WaitingForTheirStation.ContainsKey(file.Path))
            .SelectMany(Breaches)
            .ToList();

        Assert.True(broken.Count == 0, $"Words on screen break the wording rules (DESIGN.md, Words on screen):\n{string.Join('\n', broken)}");
    }

    [Fact]
    public void AFileWaitingForItsStationLeavesTheListOnceItsWordingIsFixed()
    {
        List<string> fixedAlready = WaitingForTheirStation.Keys
            .Where(path => !File.Exists(Path.Combine(RepoRoot.Path, path)) || !Breaches((path, File.ReadAllText(Path.Combine(RepoRoot.Path, path)))).Any())
            .ToList();

        Assert.Empty(fixedAlready);
    }

    private static IEnumerable<string> Breaches((string Path, string Text) file)
    {
        string[] lines = file.Text.Split('\n');
        for (int index = 0; index < lines.Length; index++)
            foreach ((Regex pattern, string rule) in Rules)
                if (pattern.IsMatch(lines[index]))
                    yield return $"{file.Path}:{index + 1}: {rule}";
    }

    private static readonly (Regex Pattern, string Rule)[] Rules =
    [
        (ExamplePlaceholder(), "no example placeholders such as \"Like 15 Nov\": a format goes in the description, or nowhere"),
        (GiveItA(), "a blocked or problem line names the field and the fix (\"Enter a name for the ministry.\"), never \"Give it a …\""),
        (EmptyColon(), "say what an empty field means in a sentence (\"Leave this empty to …\"), never \"Empty: …\""),
        (DeleteItKeepIt(), "a confirm's buttons name the thing (\"Delete the chat\", \"Keep the chat\"), never \"Delete it\" or \"Keep it\""),
        (LabelWithBrackets(), "a label names the thing; what it means goes in the description, never in brackets"),
        (LabelAsQuestion(), "a label names the thing, never as a question"),
    ];

    private static IEnumerable<(string Path, string Text)> Shipped() =>
        RepoFiles.Under("src", ".razor").Concat(RepoFiles.Under("src", ".cs"))
            .Select(path => (Path: Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'), Text: File.ReadAllText(path)))
            .Where(file => !file.Path.Contains("/obj/", StringComparison.Ordinal) && !file.Path.Contains("/bin/", StringComparison.Ordinal));

    [GeneratedRegex(@"""Like [^""]")]
    private static partial Regex ExamplePlaceholder();

    [GeneratedRegex(@"""Give it a ")]
    private static partial Regex GiveItA();

    [GeneratedRegex(@"Hint=""Empty:")]
    private static partial Regex EmptyColon();

    [GeneratedRegex(@"""(Delete|Keep) it""")]
    private static partial Regex DeleteItKeepIt();

    [GeneratedRegex(@"\bLabel=""[^""@]*\(")]
    private static partial Regex LabelWithBrackets();

    [GeneratedRegex(@"\bLabel=""[^""@]*\?""")]
    private static partial Regex LabelAsQuestion();
}
