using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class SyncOnlyRules
{
    private static readonly string[] ApprovedDirectCalls =
    [
    ];

    private static readonly string[] NotYetOnSync =
    [
    ];

    private static readonly string[] TheTransportItself =
    [
        "src/Platform/GSBC.ChurchApp.Platform.Client/Native/",
        "src/Platform/GSBC.ChurchApp.Platform.Client/Transport/",
    ];

    private const string AppHost = "src/Hosts/App/GSBC.ChurchApp.Hosts.App/GSBC.ChurchApp.Hosts.App.csproj";

    [Fact]
    public void TheAppCallsTheServerOnlyThroughSyncTheOutboxOrAnApprovedException()
    {
        Dictionary<string, string> services = ServicesByPath();
        List<string> violations = AppFiles()
            .Where(file => !file.AdminOnly && !TheTransportItself.Any(file.Path.StartsWith))
            .SelectMany(file => DirectCalls(file, services).Select(call => (file.Path, Call: call)))
            .Where(found => !ApprovedDirectCalls.Contains(found.Call) && !NotYetOnSync.Contains($"{found.Path}: {found.Call}"))
            .Select(found => $"{found.Path}: {found.Call}")
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void TheAppNeverReachesAdminOnlyCode()
    {
        List<AppFile> files = AppFiles().ToList();
        List<string> adminOnly = files.Where(file => file.AdminOnly).SelectMany(file => file.Types).Distinct().ToList();
        List<string> violations = files
            .Where(file => !file.AdminOnly)
            .SelectMany(file => adminOnly.Where(type => Regex.IsMatch(file.Text, $@"(?<![\w.""/-]){type}\b")).Select(type => $"{file.Path} uses {type}"))
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void OnlySharedCodeIsAdminOnly()
    {
        List<string> violations = AppFiles()
            .Where(file => file.AdminOnly && !file.Path.Contains(".Client/", StringComparison.Ordinal) && !file.Path.Contains("Platform.Ui/", StringComparison.Ordinal))
            .Select(file => file.Path)
            .ToList();

        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ExceptionsAreListedOnce()
    {
        Assert.Equal(ApprovedDirectCalls.Length, ApprovedDirectCalls.Distinct().Count());
        Assert.Equal(NotYetOnSync.Length, NotYetOnSync.Distinct().Count());
    }

    private static IEnumerable<string> DirectCalls(AppFile file, IReadOnlyDictionary<string, string> services)
    {
        string text = file.Text;
        HashSet<int> understood = [];
        foreach (Match proxy in ProxyField().Matches(text))
        {
            understood.Add(proxy.Groups["create"].Index);
            string service = proxy.Groups["service"].Value;
            if (proxy.Groups["access"].Value is "public" or "internal" or "protected")
            {
                yield return $"{service} handed out as {proxy.Groups["name"].Value}";
                continue;
            }
            string name = proxy.Groups["name"].Value;
            foreach (Match call in Regex.Matches(text, $@"\b{name}\.(\w+?)Async\("))
                yield return $"{service}.{call.Groups[1].Value}";
            if (Regex.Matches(text, $@"(?<![\w.]){name}\b(?!\.\w+?Async\()").Count > 1)
                yield return $"{service} handed out as {name}";
        }
        foreach (Match inline in InlineCall().Matches(text))
        {
            understood.Add(inline.Groups["create"].Index);
            yield return $"{inline.Groups["service"].Value}.{inline.Groups["method"].Value}";
        }
        foreach (Match create in Creates().Matches(text))
            if (!understood.Contains(create.Index))
                yield return $"{create.Groups["service"].Value} handed out";
        foreach (Match raw in RawCall().Matches(text))
        {
            string path = Regex.Match(text, $@"\b{raw.Groups["constant"].Value}\s*=\s*""(?<path>/[^""]+)""").Groups["path"].Value;
            string[] parts = path.TrimStart('/').Split('/');
            yield return parts.Length == 2 && services.TryGetValue(parts[0], out string? service) ? $"{service}.{parts[1]}" : $"a raw call to {raw.Groups["constant"].Value}";
        }
    }

    private static Dictionary<string, string> ServicesByPath() =>
        RepoFiles.Under("src", ".cs")
            .Where(path => path.Contains(".Contracts", StringComparison.Ordinal))
            .SelectMany(path => ServiceDeclaration().Matches(File.ReadAllText(path)))
            .ToDictionary(match => match.Groups["path"].Value, match => match.Groups["service"].Value);

    private static IEnumerable<AppFile> AppFiles() =>
        AppProjects().SelectMany(project =>
        {
            string folder = Path.GetRelativePath(RepoRoot.Path, Path.GetDirectoryName(project)!).Replace(Path.DirectorySeparatorChar, '/');
            return RepoFiles.Under(folder, ".cs").Concat(RepoFiles.Under(folder, ".razor"))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(path => AppFile.Read(path));
        });

    private static IEnumerable<string> AppProjects()
    {
        HashSet<string> seen = [];
        Stack<string> waiting = new([Path.Combine(RepoRoot.Path, AppHost)]);
        while (waiting.TryPop(out string? project))
        {
            if (!seen.Add(project))
                continue;
            foreach (string reference in Projects.ReferencesOf(project))
                waiting.Push(reference);
        }
        return seen;
    }

    private sealed record AppFile(string Path, string Text, bool AdminOnly, IReadOnlyList<string> Types)
    {
        public static AppFile Read(string path)
        {
            string text = File.ReadAllText(path);
            string relative = System.IO.Path.GetRelativePath(RepoRoot.Path, path).Replace(System.IO.Path.DirectorySeparatorChar, '/');
            bool razor = path.EndsWith(".razor", StringComparison.Ordinal);
            bool adminOnly = razor ? RazorAdminOnly().IsMatch(text) : ClassAdminOnly().IsMatch(text);
            IReadOnlyList<string> types = razor
                ? [System.IO.Path.GetFileNameWithoutExtension(path)]
                : TypeDeclaration().Matches(text).Select(match => match.Groups[1].Value).ToList();
            return new AppFile(relative, text, adminOnly, types);
        }
    }

    [GeneratedRegex(@"(?<access>private|public|internal|protected)?\s*(?:static\s+)?(?:readonly\s+)?(?<service>I\w+)\s+(?<name>\w+)\s*(?:\{\s*get;\s*\})?\s*=\s*[^;]*?(?<create>CreateGrpcService)<\k<service>>\(\)\s*;")]
    private static partial Regex ProxyField();

    [GeneratedRegex(@"(?<create>CreateGrpcService)<(?<service>I\w+)>\(\)\s*\.(?<method>\w+?)Async\(")]
    private static partial Regex InlineCall();

    [GeneratedRegex(@"CreateGrpcService<(?<service>I\w+)>")]
    private static partial Regex Creates();

    [GeneratedRegex(@"\.UnaryAsync\((?<constant>\w+)")]
    private static partial Regex RawCall();

    [GeneratedRegex(@"\[Service\(""(?<path>[\w.]+)""\)\]\s*public\s+interface\s+(?<service>I\w+)")]
    private static partial Regex ServiceDeclaration();

    [GeneratedRegex(@"^@attribute \[AdminOnly\]", RegexOptions.Multiline)]
    private static partial Regex RazorAdminOnly();

    [GeneratedRegex(@"\[AdminOnly\]\s*(?:public|internal)")]
    private static partial Regex ClassAdminOnly();

    [GeneratedRegex(@"(?:class|record|interface|struct)\s+(\w+)")]
    private static partial Regex TypeDeclaration();
}
