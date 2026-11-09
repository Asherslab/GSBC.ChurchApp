using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.People.Contracts;

[Service("gsbc.people.People")]
public interface IPeopleService
{
    [SignedIn]
    ValueTask<MyPeople> GetMineAsync(MineRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> SetMyBirthDateAsync(BirthDateRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<PersonCard> AddChildAsync(AddChildRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> AskToLinkAsync(AskRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> AnswerAsync(AnswerRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> WithdrawAsync(AskIdRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> UnlinkAsync(LinkRequest request, CallContext context = default);

    [Requires(PeoplePermissions.FamilyViewNode)]
    ValueTask<PersonFamily> GetPersonAsync(PersonRequest request, CallContext context = default);

    [Requires(PeoplePermissions.FamilyEditNode)]
    ValueTask<Done> UpdatePersonAsync(UpdatePersonRequest request, CallContext context = default);

    [Requires(PeoplePermissions.FamilyEditNode)]
    ValueTask<Done> RemoveChildAsync(PersonRequest request, CallContext context = default);

    [Requires(PeoplePermissions.SignInCodeNode)]
    ValueTask<SignInCodeReply> MakeSignInCodeAsync(PersonRequest request, CallContext context = default);

    [Requires(PeoplePermissions.FamiliesManageNode)]
    ValueTask<Done> LinkAsync(LinkRequest request, CallContext context = default);

    [Requires(PeoplePermissions.SafeguardingEditNode)]
    ValueTask<Done> SetSafeguardingAsync(SafeguardingRequest request, CallContext context = default);

    [Requires(PeoplePermissions.SafeguardingViewNode)]
    ValueTask<FlaggedList> GetFlaggedAsync(FlaggedRequest request, CallContext context = default);
}
