using System.Diagnostics;

namespace GSBC.ChurchApp.Architecture.Tests;

internal static class RepoFiles
{
    private static readonly Lazy<IReadOnlyList<string>> Listed = new(ListWithGit);

    public static IEnumerable<string> Under(string folder, string suffix = "") =>
        Listed.Value
            .Where(path => path.StartsWith($"{folder}/", StringComparison.Ordinal) && path.EndsWith(suffix, StringComparison.Ordinal))
            .Select(path => Path.Combine(RepoRoot.Path, path.Replace('/', Path.DirectorySeparatorChar)));

    private static IReadOnlyList<string> ListWithGit()
    {
        using Process git = Process.Start(new ProcessStartInfo("git", ["ls-files", "-z", "--cached", "--others", "--exclude-standard"])
        {
            WorkingDirectory = RepoRoot.Path,
            RedirectStandardOutput = true,
        }) ?? throw new InvalidOperationException("git couldn't be started.");
        string output = git.StandardOutput.ReadToEnd();
        git.WaitForExit();
        if (git.ExitCode != 0)
            throw new InvalidOperationException($"git ls-files exited with {git.ExitCode}.");
        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Where(path => File.Exists(Path.Combine(RepoRoot.Path, path)))
            .ToList();
    }
}
