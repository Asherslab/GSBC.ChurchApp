using Grpc.Core;
using GSBC.ChurchApp.Features.People.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.People.Admin;

public sealed class PeopleOfficeClient(ChannelBase channel)
{
    private readonly IPeopleService _people = channel.CreateGrpcService<IPeopleService>();

    public ValueTask<Done> LinkAsync(string parentId, string childId) => _people.LinkAsync(new LinkRequest { ParentId = parentId, ChildId = childId });

    public ValueTask<PersonFamily> GetPersonAsync(string personId) => _people.GetPersonAsync(new PersonRequest { PersonId = personId });

    public ValueTask<Done> SetSafeguardingAsync(string personId, SafeguardingRecord record) =>
        _people.SetSafeguardingAsync(new SafeguardingRequest { PersonId = personId, Record = record });

    public async Task<IReadOnlyList<FlaggedPerson>> GetFlaggedAsync() => (await _people.GetFlaggedAsync(new FlaggedRequest())).People;
}
