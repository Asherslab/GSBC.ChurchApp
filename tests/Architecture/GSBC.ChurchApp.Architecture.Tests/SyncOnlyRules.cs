using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class SyncOnlyRules
{
    private static readonly string[] ApprovedDirectCalls =
    [
        "IAccountsService.Redeem",
        "IAccountsService.StartSignUp",
        "IAccountsService.RequestRecovery",
        "IAccountsService.ResendConfirmation",
        "IAccountsService.ConfirmEmail",
        "IAccountsService.SignOutEverywhere",
        "IAccountsService.EndSession",
        "IAccountsService.DeleteMyAccount",
        "IInstallsService.Claim",
        "INotificationsService.RegisterDevice",
        "IPeopleService.MakeSignInCode",
        "IMediaService.GetLink",
        "ICalendarFilesService.Link",
        "ICalendarFilesService.ResetLink",
        "IDestinationsService.Describe",
        "IMinistryLeadingService.Find",
        "IMinistryLeadingService.Deletion",
        "IMinistryLeadingService.Moving",
        "IMinistryLeadingService.CalendarRemoval",
        "IEventEditingService.TellPeople",
        "IAccountsService.UpdateName",
        "IAccountsService.RemovePasskey",
        "IMediaService.List",
        "IMediaFoldersService.List",
    ];

    private static readonly string[] MadeOnlyByTheOutboxWorker =
    [
        "IMediaService.FindByHash",
        "IMediaService.BeginUpload",
        "IMediaService.CompleteUpload",
    ];

    private const string QueuedUploadCalls = "src/Features/Media/GSBC.ChurchApp.Features.Media.Client/MediaUploadRequests.cs";

    private static readonly string[] NotYetOnSync =
    [
    ];

    private static readonly string[] TheTransportItself =
    [
        "src/Platform/GSBC.ChurchApp.Platform.Client/Native/",
        "src/Platform/GSBC.ChurchApp.Platform.Client/Transport/",
        "src/Platform/GSBC.ChurchApp.Platform.Client/Diagnostics/ObservedChannel.cs",
    ];

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
    public void UploadsAreQueuedForTheOutboxWorkerAndNeverCalledByAScreen()
    {
        Dictionary<string, string> services = ServicesByPath();
        string text = File.ReadAllText(Path.Combine(RepoRoot.Path, QueuedUploadCalls));
        string service = services[QueuedService().Match(text).Groups["path"].Value];
        List<string> queued = QueuedMethod().Matches(text).Select(match => $"{service}.{match.Groups["method"].Value}").ToList();

        Assert.Equal(MadeOnlyByTheOutboxWorker.Order(), queued.Order());
        Assert.Empty(MadeOnlyByTheOutboxWorker.Intersect(ApprovedDirectCalls));
    }

    [Fact]
    public void TheAppNeverReachesAdminOnlyCode()
    {
        List<AppFile> files = AppFiles().ToList();
        List<(string Type, bool Component)> adminOnly = files.Where(file => file.AdminOnly)
            .SelectMany(file => file.Types.Select(type => (type, file.Path.EndsWith(".razor", StringComparison.Ordinal)))).Distinct().ToList();
        List<string> violations = files
            .Where(file => !file.AdminOnly)
            .SelectMany(file => adminOnly.Where(only => Regex.IsMatch(file.Text, Use(only.Type, only.Component))).Select(only => $"{file.Path} uses {only.Type}"))
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

    private static string Use(string type, bool component) => component
        ? $@"<{type}[\s/>]|typeof\({type}\)|<{type}>"
        : $@"(?<![\w.""/-])(?<!\bnew\s){type}\b(?!\s*\()|\bnew\s+{type}\b";

    private static IEnumerable<string> DirectCalls(AppFile file, IReadOnlyDictionary<string, string> services)
    {
        string text = file.Text;
        HashSet<int> understood = [];
        List<Match> proxies = ProxyField().Matches(text).ToList();
        foreach (Match proxy in proxies)
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
            if (Regex.Matches(text, $@"(?<![\w.]){name}\b(?!\.\w+?Async\()").Count > proxies.Count(other => other.Groups["name"].Value == name))
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
        foreach (Match _ in Invoker().Matches(text))
            yield return "a call invoker of its own";
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
        Projects.InTheApp().SelectMany(project =>
        {
            string folder = Path.GetRelativePath(RepoRoot.Path, Path.GetDirectoryName(project)!).Replace(Path.DirectorySeparatorChar, '/');
            return RepoFiles.Under(folder, ".cs").Concat(RepoFiles.Under(folder, ".razor"))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .Select(AppFile.Read);
        });

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
                : ClassAdminOnly().Matches(text).Select(match => match.Groups["type"].Value).ToList();
            return new AppFile(relative, text, adminOnly, types);
        }
    }

    [GeneratedRegex(@"\bService\s*=\s*""/(?<path>[\w.]+)/""")]
    private static partial Regex QueuedService();

    [GeneratedRegex(@"=\s*Service\s*\+\s*""(?<method>\w+)""")]
    private static partial Regex QueuedMethod();

    [GeneratedRegex(@"(?<access>private|public|internal|protected)?\s*(?:static\s+)?(?:readonly\s+)?(?<service>I\w+)\s+(?<name>\w+)\s*(?:\{\s*get;\s*\})?\s*=\s*[^;]*?(?<create>CreateGrpcService)<\k<service>>\(\)\s*;")]
    private static partial Regex ProxyField();

    [GeneratedRegex(@"(?<create>CreateGrpcService)<(?<service>I\w+)>\(\)\s*\.(?<method>\w+?)Async\(")]
    private static partial Regex InlineCall();

    [GeneratedRegex(@"CreateGrpcService<(?<service>I\w+)>")]
    private static partial Regex Creates();

    [GeneratedRegex(@"\bCreateCallInvoker\(|\bAsyncUnaryCall\(")]
    private static partial Regex Invoker();

    [GeneratedRegex(@"\.UnaryAsync\((?<constant>\w+)")]
    private static partial Regex RawCall();

    [GeneratedRegex(@"\[Service\(""(?<path>[\w.]+)""\)\]\s*public\s+interface\s+(?<service>I\w+)")]
    private static partial Regex ServiceDeclaration();

    [GeneratedRegex(@"^@attribute \[AdminOnly\]", RegexOptions.Multiline)]
    private static partial Regex RazorAdminOnly();

    [GeneratedRegex(@"\[AdminOnly\]\s*(?:public|internal)[\w\s]*?\bclass\s+(?<type>\w+)")]
    private static partial Regex ClassAdminOnly();
}
