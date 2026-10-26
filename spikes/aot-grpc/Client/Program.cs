using Contracts;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using ProtoBuf.Grpc.Client;

using var channel = GrpcChannel.ForAddress(args.Length > 0 ? args[0] : "http://localhost:5310",
    new GrpcChannelOptions { HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, new HttpClientHandler()) { HttpVersion = System.Net.HttpVersion.Version11 } });
var svc = channel.CreateGrpcService<IEventsService>();
var reply = await svc.ListAsync(new EventsRequest { Take = 3 });
foreach (var e in reply.Events) Console.WriteLine($"{e.Id} {e.Title} {e.StartsAt:o} {e.Kind} [{string.Join(",", e.Tags)}] {e.Venue?.Name} {e.Cost}");
await foreach (var e in svc.WatchAsync(new EventsRequest { Take = 2 })) Console.WriteLine($"stream {e.Id}");
