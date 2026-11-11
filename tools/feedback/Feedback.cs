#:project ../../src/Features/Feedback/GSBC.ChurchApp.Features.Feedback.Contracts/GSBC.ChurchApp.Features.Feedback.Contracts.csproj
#:project ../../src/Features/Media/GSBC.ChurchApp.Features.Media.Contracts/GSBC.ChurchApp.Features.Media.Contracts.csproj
#:package Grpc.Net.Client
#:package Grpc.Net.Client.Web
#:package protobuf-net.Grpc
#:property PublishAot=false

using System.Diagnostics;
using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Features.Media.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Client;

const string Usage = """
    usage: tools/feedback/feedback.sh --env dev|uat|local [<number>] [--status <status>] [--note "..."] [--all]

      With no number: the open feedback, newest first (--all includes closed).
      With a number: that feedback in full; its screenshots, videos and the phone's state are saved to a folder it prints.
      With --status: move it to new, in-progress, fixed (fixed, to check), resolved or not-doing, with an optional note.
      Resolved and not-doing delete its pictures, videos and phone data.
    """;

Dictionary<string, Uri> gateways = new()
{
    ["dev"] = new Uri("https://dev-app.baptist.com.au"),
    ["uat"] = new Uri("https://uat-app.baptist.com.au"),
    ["local"] = new Uri("http://localhost:5400"),
};

Dictionary<string, TicketStatus> statuses = new(StringComparer.OrdinalIgnoreCase)
{
    ["new"] = TicketStatus.New,
    ["in-progress"] = TicketStatus.InProgress,
    ["fixed"] = TicketStatus.FixedToCheck,
    ["resolved"] = TicketStatus.Resolved,
    ["not-doing"] = TicketStatus.NotDoing,
};

string? environment = null;
int? number = null;
string? statusText = null;
string note = "";
bool all = false;
for (int index = 0; index < args.Length; index++)
{
    switch (args[index])
    {
        case "--env" when index + 1 < args.Length:
            environment = args[++index];
            break;
        case "--status" when index + 1 < args.Length:
            statusText = args[++index];
            break;
        case "--note" when index + 1 < args.Length:
            note = args[++index];
            break;
        case "--all":
            all = true;
            break;
        case "-h" or "--help":
            Console.WriteLine(Usage);
            return 0;
        case var other when int.TryParse(other.TrimStart('#'), out int parsed) && number is null:
            number = parsed;
            break;
        default:
            Console.Error.WriteLine(Usage);
            return 2;
    }
}

if (environment is null || !gateways.TryGetValue(environment, out Uri? gateway))
{
    Console.Error.WriteLine(environment is null ? Usage : $"There's no gateway for '{environment}' yet. Add it to the list at the top of tools/feedback/Feedback.cs.");
    return 2;
}

TicketStatus? newStatus = null;
if (statusText is not null)
{
    if (number is null || !statuses.TryGetValue(statusText, out TicketStatus parsedStatus))
    {
        Console.Error.WriteLine(number is null ? "Say which feedback, by its number." : $"'{statusText}' isn't a status. Use one of: {string.Join(", ", statuses.Keys)}.");
        return 2;
    }
    newStatus = parsedStatus;
}

if (KeychainSecret($"gsbc-tool-{environment}") is not { } secret)
{
    Console.Error.WriteLine($"No tool is connected for {environment}. Connect one on the admin panel's Account page ({gateway}account), then run tools/health/connect.sh {environment}.");
    return 1;
}

using GrpcChannel channel = GrpcChannel.ForAddress(gateway, new GrpcChannelOptions
{
    HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()),
    HttpVersion = HttpVersion.Version11,
    HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher,
});
IFeedbackDeskService desk = channel.CreateGrpcService<IFeedbackDeskService>();
IMediaService media = channel.CreateGrpcService<IMediaService>();
CallContext signedIn = new(new CallOptions(new Metadata { { "X-Gsbc-Session", secret } }));

try
{
    if (number is not { } wanted)
    {
        PrintList(environment, await desk.ListAsync(new DeskQuery { Filter = all ? DeskFilter.Everything : DeskFilter.Open }, signedIn));
        return 0;
    }

    Ticket ticket = await desk.TicketAsync(new TicketRef { Number = wanted }, signedIn);
    if (newStatus is { } status)
    {
        await desk.SetStatusAsync(new StatusChange { Id = ticket.Line.Id, Status = status, Note = note }, signedIn);
        Console.WriteLine($"#{wanted} is now {Name(status)}.{(note.Length > 0 ? " The note was added." : "")}"
            + (status is TicketStatus.Resolved or TicketStatus.NotDoing && ticket.Attachments.Count > 0 ? " Its pictures, videos and phone data were deleted." : ""));
        return 0;
    }

    string folder = Path.Combine(Path.GetTempPath(), "gsbc-feedback", $"{environment}-{wanted}");
    Directory.CreateDirectory(folder);
    List<string> saved = [];
    using HttpClient download = new();
    int position = 0;
    foreach (MediaRef attachment in ticket.Attachments)
        saved.Add(await SaveAsync(attachment, $"{++position:00}-{SafeName(attachment.FileName, attachment.Key)}"));
    if (ticket.PhoneData is { } data)
        saved.Add(await SaveAsync(data, "phone-data.sqlite"));
    if (ticket.State.Length > 0)
    {
        string state = Path.Combine(folder, "phone-state.json");
        await File.WriteAllTextAsync(state, ticket.State);
        saved.Add(state);
    }

    PrintTicket(ticket, folder, saved);
    return 0;

    async Task<string> SaveAsync(MediaRef file, string name)
    {
        MediaLink link = await media.GetLinkAsync(new LinkRequest { Id = file.Id, Key = file.Key }, signedIn);
        string path = Path.Combine(folder, name);
        await using Stream body = await download.GetStreamAsync(link.Url);
        await using FileStream target = File.Create(path);
        await body.CopyToAsync(target);
        return path;
    }
}
catch (RpcException exception) when (exception.StatusCode == StatusCode.Unauthenticated)
{
    Console.Error.WriteLine($"The tool's session for {environment} has ended or expired (30 days unused). Connect a tool again on {gateway}account, then run tools/health/connect.sh {environment}.");
    return 1;
}
catch (RpcException exception) when (exception.StatusCode == StatusCode.PermissionDenied)
{
    Console.Error.WriteLine($"This account can't look after feedback on {environment}. It needs the Feedback Triage role.");
    return 1;
}
catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
{
    Console.Error.WriteLine($"There's no feedback #{number} on {environment}.");
    return 1;
}
catch (RpcException exception)
{
    Console.Error.WriteLine($"{environment}: {exception.StatusCode}: {exception.Status.Detail}");
    return 1;
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

static void PrintList(string environment, DeskList list)
{
    Console.WriteLine($"Feedback on {environment}: {Count(list, TicketStatus.New)} new, {Count(list, TicketStatus.InProgress)} in progress, {Count(list, TicketStatus.FixedToCheck)} fixed, to check");
    Console.WriteLine();
    if (list.Tickets.Count == 0)
        Console.WriteLine("Nothing here.");
    foreach (TicketLine line in list.Tickets.OrderByDescending(line => line.SentAt))
    {
        Console.WriteLine($"  #{line.Number,-4} {Name(line.Status),-16} {When(line.SentAt)}  {line.From}");
        Console.WriteLine($"        {FirstLine(line.Text)}");
        Console.WriteLine($"        {Details(line)}");
    }
    if (list.Tickets.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"One in full, with its files: tools/feedback/feedback.sh --env {environment} <number>");
    }
}

static void PrintTicket(Ticket ticket, string folder, IReadOnlyList<string> saved)
{
    TicketLine line = ticket.Line;
    PhoneDetails phone = ticket.Phone ?? new PhoneDetails();
    Console.WriteLine($"#{line.Number}  {Name(line.Status)}");
    Console.WriteLine($"  from:     {line.From}");
    Console.WriteLine($"  sent:     {When(line.SentAt)}");
    Console.WriteLine($"  app:      {phone.AppVersion} ({phone.AppBuild}), {ticket.Environment}{(phone.DevelopmentBuild ? ", a development build" : "")}");
    Console.WriteLine($"  phone:    {PhoneModels.Name(phone.Model)} ({phone.Model}), {phone.Platform} {phone.OsVersion}");
    Console.WriteLine($"  page:     {(ticket.Page.Length > 0 ? ticket.Page : "(not known)")}");
    Console.WriteLine($"  phone id: {(ticket.InstallId.Length > 0 ? $"{ticket.InstallId}, {ticket.FromThisPhone} sent{(ticket.Blocked ? $", blocked: {ticket.BlockedBecause}" : "")}" : "(not a verified phone)")}");
    Console.WriteLine();
    Console.WriteLine(line.Text.Length > 0 ? line.Text : "(no words)");
    if (ticket.Notes.Count + ticket.History.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("What's happened");
        foreach ((DateTime at, string text) in ticket.History.Select(step => (step.At, $"{step.Who} {step.What.ToLowerInvariant()}"))
                     .Concat(ticket.Notes.Select(note => (note.At, $"{note.Author}: {note.Text}"))).OrderBy(item => item.Item1))
            Console.WriteLine($"  {When(at)}  {text}");
    }
    Console.WriteLine();
    if (line.AttachmentsDeleted)
        Console.WriteLine("Its pictures, videos and phone data were deleted when it closed.");
    else if (ticket.HadPhoneData && ticket.PhoneData is null)
        Console.WriteLine("A copy of the phone's stored data came with it; downloading it needs the phone data permission.");
    Console.WriteLine(saved.Count == 0 ? "No files with it." : $"Saved to {folder}:");
    foreach (string path in saved)
        Console.WriteLine($"  {Path.GetFileName(path)}  ({new FileInfo(path).Length / 1024} KB)");
}

static int Count(DeskList list, TicketStatus status) => list.Counts.FirstOrDefault(count => count.Status == status)?.Count ?? 0;

static string Name(TicketStatus status) => status switch
{
    TicketStatus.New => "new",
    TicketStatus.InProgress => "in progress",
    TicketStatus.FixedToCheck => "fixed, to check",
    TicketStatus.Resolved => "resolved",
    _ => "not doing",
};

static string Details(TicketLine line) =>
    string.Join(" · ", new[] { line.AppBuild, line.Model, line.Attachments > 0 ? $"{line.Attachments} attached" : "", line.AttachmentsDeleted ? "files deleted" : "" }
        .Where(part => part.Length > 0));

static string FirstLine(string text)
{
    string first = text.Split('\n', 2)[0].Trim();
    return first.Length == 0 ? "(screenshots, no words)" : first.Length <= 100 ? first : $"{first[..97]}…";
}

static string SafeName(string fileName, string key)
{
    string name = string.Concat((fileName.Length > 0 ? fileName : key).Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '-'));
    return name.Length > 0 ? name : "file";
}

static string When(DateTime at) => DateTime.SpecifyKind(at, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm");
