using Grpc.Core;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Prayer.Client;

[AdminOnly]
public sealed class PastoralClient(ChannelBase channel)
{
    private readonly IPastoralService _pastoral = channel.CreateGrpcService<IPastoralService>();

    public async Task<Inbox> InboxAsync(string search) => await _pastoral.InboxAsync(new InboxRequest { Search = search });

    public async Task<RequestDetails> RequestAsync(string id) => await _pastoral.RequestAsync(new RequestRef { Id = id });

    public async Task CareAsync(string id, CareState care, string answeredNote = "") =>
        await _pastoral.CareAsync(new CareChange { Id = id, Care = care, AnsweredNote = answeredNote });

    public async Task AddNoteAsync(string id, string text) => await _pastoral.AddNoteAsync(new NoteSave { RequestId = id, Text = text });

    public async Task DeleteNoteAsync(string id, string noteId) => await _pastoral.DeleteNoteAsync(new NoteRef { RequestId = id, NoteId = noteId });

    public async Task ShowOnWallAsync(string id, string text, bool showName, DateTime until) =>
        await _pastoral.ShowOnWallAsync(new WallSave { Id = id, Text = text, ShowName = showName, Until = until });

    public async Task KeepPrivateAsync(string id) => await _pastoral.KeepPrivateAsync(new RequestRef { Id = id });

    public async Task DeleteAsync(string id) => await _pastoral.DeleteAsync(new RequestRef { Id = id });
}
