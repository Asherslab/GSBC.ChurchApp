using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class SessionSecretRules
{
    private static readonly string[] WebViewCode = [".cs", ".razor", ".js"];
    private const string NativePlugin = "src/Hosts/App/native/data-layer/ios/Sources/DataLayerPlugin/DataLayerPlugin.swift";

    [Fact]
    public void NothingThatRunsInTheWebViewHandlesTheSessionSecret()
    {
        List<string> violations = RepoFiles.Under("src")
            .Select(path => Path.GetRelativePath(RepoRoot.Path, path).Replace(Path.DirectorySeparatorChar, '/'))
            .Where(path => WebViewCode.Contains(Path.GetExtension(path)) && RunsInTheWebView().IsMatch(path))
            .Where(path => HandlesTheSecret().IsMatch(File.ReadAllText(Path.Combine(RepoRoot.Path, path))))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void TheNativePluginNeverReadsTheSessionOutForTheWebView()
    {
        string plugin = File.ReadAllText(Path.Combine(RepoRoot.Path, NativePlugin));

        Assert.DoesNotMatch(ReadsTheVault(), plugin);
        Assert.DoesNotMatch(TakesASecretArgument(), plugin);
    }

    [GeneratedRegex(@"^src/(Hosts/App/GSBC\.ChurchApp\.Hosts\.App|Platform/GSBC\.ChurchApp\.Platform\.(Client|Ui)|Features/[^/]+/GSBC\.ChurchApp\.Features\.[^/]+\.(App|Client))/")]
    private static partial Regex RunsInTheWebView();

    [GeneratedRegex("X-Gsbc-Session|secret", RegexOptions.IgnoreCase)]
    private static partial Regex HandlesTheSecret();

    [GeneratedRegex(@"vault\.read\(\)|SessionKeychain\(\)\.read", RegexOptions.IgnoreCase)]
    private static partial Regex ReadsTheVault();

    [GeneratedRegex(@"getString\(""(secret|session)""\)")]
    private static partial Regex TakesASecretArgument();
}
