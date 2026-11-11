using Grpc.Core;
using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Chats.Client;

[AdminOnly]
public sealed class OversightClient(ChannelBase channel)
{
    private readonly IChatOversightService _oversight = channel.CreateGrpcService<IChatOversightService>();
    private readonly IChatsService _chats = channel.CreateGrpcService<IChatsService>();

    public async Task<Desk> DeskAsync() => await _oversight.DeskAsync(new DeskRequest());

    public async Task<CheckedChat> CheckAsync(string chatId, string reason) => await _oversight.CheckAsync(new CheckRequest { ChatId = chatId, Reason = reason });

    public async Task<Log> LogAsync(string chatId = "") => await _oversight.LogAsync(new LogRequest { ChatId = chatId });

    public async Task<Reports> ReportsAsync() => await _oversight.ReportsAsync(new Nothing());

    public async Task HandleAsync(string reportId, string outcome) => await _oversight.HandleAsync(new HandleReport { ReportId = reportId, Outcome = outcome });

    public async Task<PausedList> PausedAsync() => await _oversight.PausedAsync(new Nothing());

    public async Task PauseAsync(string personId, bool paused, string reason) =>
        await _oversight.PauseAsync(new PauseRequest { PersonId = personId, Paused = paused, Reason = reason });

    public async Task StartAsync(StartChat request) => await _chats.StartAsync(request);

    public async Task DeleteAsync(string chatId) => await _chats.DeleteAsync(new ChatRef { ChatId = chatId });
}
