using System.Net;
using Contracts;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using ProtoBuf.Grpc.Client;

// A .NET client (e.g. the admin panel) can use either transport: native gRPC over HTTP/2, or gRPC-Web
// over HTTP/1.1 (what a browser falls back to). Both are exercised here.
var web = args.Length > 0 ? args[0] : "http://localhost:5310";
var native = args.Length > 1 ? args[1] : "http://localhost:5312";

await Run("native gRPC, HTTP/2", GrpcChannel.ForAddress(native));
await Run("gRPC-Web, HTTP/1.1", GrpcChannel.ForAddress(web, new GrpcChannelOptions
{
    HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler()),
    HttpVersion = HttpVersion.Version11,
}));

static async Task Run(string transport, GrpcChannel channel)
{
    using (channel)
    {
        Console.WriteLine($"-- {transport}");
        var svc = channel.CreateGrpcService<IEventsService>(); // rewritten to the generated proxy (AppClients)
        var reply = await svc.ListAsync(new EventsRequest { Take = 3 });
        foreach (var e in reply.Events) Console.WriteLine($"{e.Id} {e.Title} {e.StartsAt:o} ({e.StartsAt.Kind}) {e.Kind} [{string.Join(",", e.Tags)}] {e.Venue?.Name} {e.Cost}");
        await foreach (var e in svc.WatchAsync(new EventsRequest { Take = 2 })) Console.WriteLine($"stream {e.Id}");
    }
}
