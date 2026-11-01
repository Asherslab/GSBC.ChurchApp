namespace GSBC.ChurchApp.Architecture.Tests;

internal static class RepoRoot
{
    public static string Path { get; } = Find(AppContext.BaseDirectory);

    private static string Find(string directory) =>
        File.Exists(System.IO.Path.Combine(directory, "GSBC.ChurchApp.slnx"))
            ? directory
            : Find(Directory.GetParent(directory)?.FullName ?? throw new InvalidOperationException("GSBC.ChurchApp.slnx not found."));
}
