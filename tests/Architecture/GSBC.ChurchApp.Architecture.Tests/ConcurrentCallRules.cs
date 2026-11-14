using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class ConcurrentCallRules
{
    [Fact]
    public void CallsStartedTogetherAreAwaitedTogether()
    {
        List<string> apart = [];
        foreach (string path in RepoFiles.Under("src", ".cs").Concat(RepoFiles.Under("src", ".razor")))
        {
            string[] lines = File.ReadAllLines(path);
            for (int line = 0; line < lines.Length; line++)
            {
                int started = line;
                while (started < lines.Length && StartedCall().IsMatch(lines[started]))
                    started++;
                if (started - line >= 2 && lines.Skip(started).FirstOrDefault(text => text.Contains("await ", StringComparison.Ordinal)) is { } firstAwait
                    && !firstAwait.Contains("Task.WhenAll(", StringComparison.Ordinal))
                    apart.Add($"{Path.GetRelativePath(RepoRoot.Path, path)}:{line + 1}");
                line = Math.Max(line, started - 1);
            }
        }

        Assert.True(apart.Count == 0,
            "These start several calls and then await them one by one: when an early one fails, the later ones fail unobserved and are reported as crashes. Await them with Task.WhenAll first:"
            + Environment.NewLine + string.Join(Environment.NewLine, apart));
    }

    [GeneratedRegex(@"^\s*(?:Value)?Task<[^=]+>\s+\w+\s*=\s*(?!Task\.(?:FromResult|CompletedTask))[^;]*\([^;]*;\s*$")]
    private static partial Regex StartedCall();
}
