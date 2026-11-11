using Grpc.Core;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Feedback.Client;

[AdminOnly]
public sealed class FeedbackDeskClient(ChannelBase channel)
{
    private readonly IFeedbackDeskService _desk = channel.CreateGrpcService<IFeedbackDeskService>();

    public async Task<DeskList> ListAsync(DeskFilter filter, string search = "", SourceFilter source = SourceFilter.Both) =>
        await _desk.ListAsync(new DeskQuery { Filter = filter, Search = search, Source = source });

    public async Task<Ticket> TicketAsync(int number) => await _desk.TicketAsync(new TicketRef { Number = number });

    public async Task SetStatusAsync(string id, TicketStatus status, string note = "") =>
        await _desk.SetStatusAsync(new StatusChange { Id = id, Status = status, Note = note });

    public async Task AddNoteAsync(string id, string text) => await _desk.AddNoteAsync(new NoteSave { TicketId = id, Text = text });

    public async Task DeleteNoteAsync(string id, string noteId) => await _desk.DeleteNoteAsync(new NoteRef { TicketId = id, NoteId = noteId });

    public async Task DeleteAsync(string id) => await _desk.DeleteAsync(new TicketRef { Id = id });

    public async Task BlockAsync(string id, string reason) => await _desk.BlockAsync(new BlockSave { TicketId = id, Reason = reason });

    public async Task UnblockAsync(string installId) => await _desk.UnblockAsync(new PhoneRef { InstallId = installId });

    public async Task<BlockedPhones> BlockedAsync() => await _desk.BlockedAsync(new BlockedQuery());
}
