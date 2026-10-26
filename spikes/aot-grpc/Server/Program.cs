using System.Runtime.CompilerServices;
using Contracts;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Server;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Services.AddServerServices();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
    .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding")));
var app = builder.Build();
app.UseCors();
app.UseGrpcWeb(new() { DefaultEnabled = true });
app.MapGrpcService<EventsService>();
app.MapGet("/", () => "ok");
app.Run();

sealed class EventsService : IEventsService
{
    static List<ChurchEvent> Make(int n) => Enumerable.Range(1, n).Select(i => new ChurchEvent
    {
        Id = $"e{i}", Title = $"Event {i}", StartsAt = new DateTime(2026, 11, i, 10, 0, 0, DateTimeKind.Utc),
        Kind = (EventKind)(i % 3), Tags = ["a", "b"], Venue = new Venue { Name = "Hall" }, Cost = i * 1.5m,
    }).ToList();

    public Task<EventsReply> ListAsync(EventsRequest request, CallContext context = default)
        => Task.FromResult(new EventsReply { Events = Make(request.Take) });

    public async IAsyncEnumerable<ChurchEvent> WatchAsync(EventsRequest request, CallContext context = default)
    {
        foreach (var e in Make(request.Take)) { await Task.Yield(); yield return e; }
    }
}
