using System.Diagnostics.CodeAnalysis;
using Grpc.Core;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Client.Feeds;
using GSBC.ChurchApp.Platform.Client.Outbox;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.People.Client;

public sealed class PeopleClient(ChannelBase channel, IWrites writes, IServiceProvider services)
{
    private const string Service = "/gsbc.people.People/";
    private readonly IPeopleService _people = channel.CreateGrpcService<IPeopleService>();

    public async Task<PersonFamily> GetMineAsync()
    {
        if (services.GetService(typeof(ILocalFeeds)) is not ILocalFeeds { IsAvailable: true } feeds)
            return new PersonFamily();
        IReadOnlyList<StoredRecord> mine = await feeds.ReadPageAsync(new FeedPageQuery(PeopleFeeds.Me, Limit: 1));
        return mine.Count == 0 ? new PersonFamily() : PeopleContractsModel.Instance.Deserialize<MyPeople>(new MemoryStream(mine[0].Payload)).Me;
    }

    public ValueTask<Done> SetMyBirthDateAsync(DateOnly? born, bool adult) =>
        SendAsync("SetMyBirthDate", new BirthDateRequest { BirthDate = WireDates.ToWire(born), Adult = adult });

    public async ValueTask<PersonCard> AddChildAsync(string name, DateOnly born, string? parentId = null)
    {
        string id = Guid.CreateVersion7().ToString();
        await SendAsync("AddChild", new AddChildRequest { Id = id, Name = name, BirthDate = WireDates.ToWire(born), ParentId = parentId ?? "" });
        return new PersonCard { Id = id, Name = name, BirthDate = WireDates.ToWire(born), AddedByFamily = true, How = LinkHow.Added };
    }

    public ValueTask<Done> AskToLinkAsync(string parentEmail, string? childId = null) =>
        SendAsync("AskToLink", new AskRequest { ParentEmail = parentEmail, ChildId = childId ?? "" });

    public ValueTask<Done> AnswerAsync(string askId, bool accept) => SendAsync("Answer", new AnswerRequest { AskId = askId, Accept = accept });

    public ValueTask<Done> WithdrawAsync(string askId) => SendAsync("Withdraw", new AskIdRequest { AskId = askId });

    public ValueTask<Done> UnlinkAsync(string parentId, string childId) => SendAsync("Unlink", new LinkRequest { ParentId = parentId, ChildId = childId });

    public ValueTask<Done> RenameAsync(string personId, string name) => SendAsync("UpdatePerson", new UpdatePersonRequest { PersonId = personId, Name = name });

    public ValueTask<Done> SetBirthDateAsync(string personId, DateOnly? born) =>
        SendAsync("UpdatePerson", new UpdatePersonRequest { PersonId = personId, BirthDate = WireDates.ToWire(born), ClearBirthDate = born is null });

    public ValueTask<Done> SetReachAsync(string personId, string reach) =>
        SendAsync("UpdatePerson", new UpdatePersonRequest { PersonId = personId, Reach = reach });

    public ValueTask<Done> RemoveChildAsync(string personId) => SendAsync("RemoveChild", new PersonRequest { PersonId = personId });

    public ValueTask<SignInCodeReply> MakeSignInCodeAsync(string personId) => _people.MakeSignInCodeAsync(new PersonRequest { PersonId = personId });

    private async ValueTask<Done> SendAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string method, T request)
    {
        await writes.SendAsync(Service + method, request, PeopleContractsModel.Instance);
        return new Done();
    }
}
