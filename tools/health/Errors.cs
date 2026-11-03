#:project ../../src/Features/Analytics/GSBC.ChurchApp.Features.Analytics.Contracts/GSBC.ChurchApp.Features.Analytics.Contracts.csproj
#:package Grpc.Net.Client
#:package Grpc.Net.Client.Web
#:package protobuf-net.Grpc
#:property PublishAot=false

using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GSBC.ChurchApp.Features.Analytics.Contracts;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;

const string Usage = """
    usage: tools/health/errors.sh --env dev|uat [--days N] [<error id>]

      With no id: the kinds of error and the failed calls of the last N days (7 by default, 30 at most).
      With an id (or the start of one, from the list): that error in full.
    """;

Dictionary<string, Uri> gateways = new()
{
    ["dev"] = new Uri("https://dev-app.baptist.com.au"),
    ["uat"] = new Uri("https://uat-app.baptist.com.au"),
};

string? environment = null;
int days = 7;
string? wanted = null;
for (int index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case "--env" when index + 1 < args.Length:
            environment = args[++index];
            break;
        case "--days" when index + 1 < args.Length && int.TryParse(args[index + 1], out int parsed):
            days = parsed;
            index++;
            break;
        case "-h" or "--help":
            Console.WriteLine(Usage);
            return 0;
        case var other when !other.StartsWith('-') && wanted is null:
            wanted = other;
            break;
        default:
            Console.Error.WriteLine(Usage);
            return 2;
    }
}

if (environment is null || !gateways.TryGetValue(environment, out Uri? gateway))
{
    Console.Error.WriteLine(environment is null ? Usage : $"There's no gateway for '{environment}' yet. Add it to the list at the top of tools/health/Errors.cs.");
    return 2;
}

if (KeychainSecret($"gsbc-tool-{environment}") is not { } secret)
{
    Console.Error.WriteLine($"No tool is connected for {environment}. Connect one on the admin panel's Account page ({gateway}account), then run tools/health/connect.sh {environment}.");
    return 1;
}

using GrpcChannel channel = GrpcChannel.ForAddress(gateway, new GrpcChannelOptions
{
    HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()),
});
IHealthService health = channel.CreateGrpcService<IHealthService>();
CallContext signedIn = new(new CallOptions(new Metadata { { "X-Gsbc-Session", secret } }));

try
{
    if (wanted is null)
        PrintOverview(environment, days, await health.GetOverviewAsync(new HealthQuery { Days = days }, signedIn));
    else if (await ResolveAsync(wanted) is { } id)
        PrintErrorKind(await health.GetErrorKindAsync(new ErrorKindRequest { Id = id }, signedIn));
    else
        return 1;
    return 0;
}
catch (RpcException exception) when (exception.StatusCode == StatusCode.Unauthenticated)
{
    Console.Error.WriteLine($"The tool's session for {environment} has ended or expired (30 days unused). Connect a tool again on {gateway}account, then run tools/health/connect.sh {environment}.");
    return 1;
}
catch (RpcException exception) when (exception.StatusCode == StatusCode.PermissionDenied)
{
    Console.Error.WriteLine($"This account can't see App health on {environment}. It needs the Developer role.");
    return 1;
}
catch (RpcException exception)
{
    Console.Error.WriteLine($"{environment}: {exception.StatusCode}: {exception.Status.Detail}");
    return 1;
}

async Task<Guid?> ResolveAsync(string text)
{
    if (Guid.TryParse(text, out Guid exact))
        return exact;

    HealthOverview everything = await health.GetOverviewAsync(new HealthQuery { Days = 30 }, signedIn);
    List<ErrorKindSummary> matches = everything.Kinds.Where(kind => kind.Id.ToString().StartsWith(text, StringComparison.OrdinalIgnoreCase)).ToList();
    if (matches.Count == 1)
        return matches[0].Id;

    Console.Error.WriteLine(matches.Count == 0
        ? $"No error kind in the last 30 days starts with '{text}'."
        : $"'{text}' matches {matches.Count} error kinds; give more of the id.");
    return null;
}

static string? KeychainSecret(string service)
{
    using Process? security = Process.Start(new ProcessStartInfo("security", ["find-generic-password", "-s", service, "-w"])
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    });
    if (security is null)
        return null;
    string found = security.StandardOutput.ReadToEnd().Trim();
    security.WaitForExit();
    return security.ExitCode == 0 && found.Length > 0 ? found : null;
}

static void PrintOverview(string environment, int days, HealthOverview overview)
{
    Console.WriteLine($"App health on {environment} ({overview.Environment}), the last {Math.Clamp(days, 1, 30)} days");
    Console.WriteLine();
    Console.WriteLine(overview.Kinds.Count == 0 ? "No errors." : $"Errors ({overview.Kinds.Count} kinds, most recent first)");
    foreach (ErrorKindSummary kind in overview.Kinds.OrderByDescending(kind => kind.LastSeen))
    {
        Console.WriteLine($"  {kind.Id.ToString()[..8]}  {kind.Count,5}×  {Origin(kind)}  {kind.ExceptionType}");
        Console.WriteLine($"            {kind.Where}");
        Console.WriteLine($"            last {When(kind.LastSeen)}, first {When(kind.FirstSeen)}; {Versions(kind)}");
    }

    Console.WriteLine();
    Console.WriteLine(overview.FailedCalls.Count == 0 ? "No failed calls." : "Failed calls");
    foreach (FailedCallSummary call in overview.FailedCalls.OrderByDescending(call => call.LastDay))
        Console.WriteLine($"  {call.Count,5}×  {call.Origin,-6}  {call.Method}  {(StatusCode)call.Status} ({call.Status}), last {Day(call.LastDay)}");

    if (overview.Kinds.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"One in full: tools/health/errors.sh --env {environment} <id>");
    }
}

static void PrintErrorKind(ErrorKindDetail detail)
{
    ErrorKindSummary kind = detail.Kind ?? throw new InvalidOperationException("The server sent an error kind with no summary.");
    Console.WriteLine(kind.ExceptionType);
    Console.WriteLine($"  id:         {kind.Id}");
    Console.WriteLine($"  where:      {kind.Where}");
    Console.WriteLine($"  from:       {Origin(kind)}");
    Console.WriteLine($"  count:      {kind.Count}");
    Console.WriteLine($"  first seen: {When(kind.FirstSeen)}");
    Console.WriteLine($"  last seen:  {When(kind.LastSeen)}");
    Console.WriteLine($"  versions:   {Versions(kind)}");
    Section("Frames", detail.Frames);
    Section("Routes", detail.Routes.Select(Counted));
    Section("Platforms", detail.Platforms.Select(Counted));
    Section("Builds", detail.Builds.Select(Counted));
    Section("By day", detail.Days.Select(day => $"{Day(day.Day)}  {day.Count}"));
}

static void Section(string title, IEnumerable<string> lines)
{
    List<string> all = lines.ToList();
    Console.WriteLine();
    Console.WriteLine($"{title}{(all.Count == 0 ? ": none" : "")}");
    foreach (string line in all)
        Console.WriteLine($"  {line}");
}

static string Counted(CountByLabel label) => $"{label.Count,5}×  {(label.Label.Length == 0 ? "(none)" : label.Label)}";

static string Origin(ErrorKindSummary kind) => kind.NativeCrash ? $"{kind.Origin} (native crash)" : kind.Origin.ToString();

static string Versions(ErrorKindSummary kind) =>
    $"{(kind.AppVersions.Count == 0 ? "no version" : string.Join(", ", kind.AppVersions))}{(kind.OnlyDevelopmentBuilds ? " (development builds only)" : "")}";

static string When(DateTime at) => DateTime.SpecifyKind(at, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");

static string Day(int dayNumber) => DateOnly.FromDayNumber(dayNumber).ToString("yyyy-MM-dd");
