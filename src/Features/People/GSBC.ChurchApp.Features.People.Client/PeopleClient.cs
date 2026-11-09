using Grpc.Core;
using GSBC.ChurchApp.Features.People.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.People.Client;

public sealed class PeopleClient(ChannelBase channel)
{
    private readonly IPeopleService _people = channel.CreateGrpcService<IPeopleService>();

    public async Task<PersonFamily> GetMineAsync() => (await _people.GetMineAsync(new MineRequest())).Me;

    public ValueTask<Done> SetMyBirthDateAsync(DateOnly? born, bool adult) =>
        _people.SetMyBirthDateAsync(new BirthDateRequest { BirthDate = WireDates.ToWire(born), Adult = adult });

    public ValueTask<PersonCard> AddChildAsync(string name, DateOnly born, string? parentId = null) =>
        _people.AddChildAsync(new AddChildRequest { Name = name, BirthDate = WireDates.ToWire(born), ParentId = parentId ?? "" });

    public ValueTask<Done> AskToLinkAsync(string parentEmail, string? childId = null) =>
        _people.AskToLinkAsync(new AskRequest { ParentEmail = parentEmail, ChildId = childId ?? "" });

    public ValueTask<Done> AnswerAsync(string askId, bool accept) => _people.AnswerAsync(new AnswerRequest { AskId = askId, Accept = accept });

    public ValueTask<Done> WithdrawAsync(string askId) => _people.WithdrawAsync(new AskIdRequest { AskId = askId });

    public ValueTask<Done> UnlinkAsync(string parentId, string childId) => _people.UnlinkAsync(new LinkRequest { ParentId = parentId, ChildId = childId });

    public ValueTask<Done> LinkAsync(string parentId, string childId) => _people.LinkAsync(new LinkRequest { ParentId = parentId, ChildId = childId });

    public ValueTask<PersonFamily> GetPersonAsync(string personId) => _people.GetPersonAsync(new PersonRequest { PersonId = personId });

    public ValueTask<Done> RenameAsync(string personId, string name) => _people.UpdatePersonAsync(new UpdatePersonRequest { PersonId = personId, Name = name });

    public ValueTask<Done> SetBirthDateAsync(string personId, DateOnly? born) =>
        _people.UpdatePersonAsync(new UpdatePersonRequest { PersonId = personId, BirthDate = WireDates.ToWire(born), ClearBirthDate = born is null });

    public ValueTask<Done> RemoveChildAsync(string personId) => _people.RemoveChildAsync(new PersonRequest { PersonId = personId });

    public ValueTask<SignInCodeReply> MakeSignInCodeAsync(string personId) => _people.MakeSignInCodeAsync(new PersonRequest { PersonId = personId });

    public ValueTask<Done> SetSafeguardingAsync(string personId, SafeguardingRecord record) =>
        _people.SetSafeguardingAsync(new SafeguardingRequest { PersonId = personId, Record = record });

    public async Task<IReadOnlyList<FlaggedPerson>> GetFlaggedAsync() => (await _people.GetFlaggedAsync(new FlaggedRequest())).People;
}
