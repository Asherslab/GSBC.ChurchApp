using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Accounts.Contracts;

[Service("gsbc.accounts.Accounts")]
public interface IAccountsService
{
    [Public]
    ValueTask<TicketReply> StartSignUpAsync(StartSignUpRequest request, CallContext context = default);

    [Public]
    ValueTask<TicketReply> RedeemAsync(RedeemRequest request, CallContext context = default);

    [Public]
    ValueTask<CodeSession> SignInWithCodeAsync(SignInWithCodeRequest request, CallContext context = default);

    [Public]
    ValueTask<Done> RequestRecoveryAsync(RequestRecoveryRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Profile> GetProfileAsync(Nothing request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> UpdateNameAsync(UpdateNameRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> ConfirmEmailAsync(ConfirmEmailRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> ResendConfirmationAsync(Nothing request, CallContext context = default);

    [SignedIn]
    ValueTask<PasskeyList> ListPasskeysAsync(Nothing request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> RemovePasskeyAsync(RemovePasskeyRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<SessionList> ListSessionsAsync(Nothing request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> EndSessionAsync(EndSessionRequest request, CallContext context = default);

    [Requires(AccountsPermissions.ConnectToolNode)]
    ValueTask<ToolConnection> ConnectToolAsync(ConnectToolRequest request, CallContext context = default);

    [SignedIn]
    ValueTask<Done> SignOutEverywhereAsync(Nothing request, CallContext context = default);

    [Requires(AccountsPermissions.ViewNode)]
    ValueTask<PeopleList> SearchPeopleAsync(SearchPeopleRequest request, CallContext context = default);

    [Requires(AccountsPermissions.ViewNode)]
    ValueTask<PeopleList> GetPeopleAsync(GetPeopleRequest request, CallContext context = default);

    [Requires(AccountsPermissions.ManageNode)]
    ValueTask<Done> InviteAsync(InviteRequest request, CallContext context = default);

    [Requires(AccountsPermissions.ManageNode)]
    ValueTask<Done> SetDisabledAsync(SetDisabledRequest request, CallContext context = default);

    [Requires(AccountsPermissions.ManageNode)]
    ValueTask<AuditList> ListAuditAsync(AuditQuery request, CallContext context = default);

    [Requires(AccountsPermissions.ManageNode)]
    ValueTask<CodeReply> IssueRecoveryCodeAsync(IssueRecoveryCodeRequest request, CallContext context = default);

    [Requires(AccountsPermissions.ReviewAccessNode)]
    ValueTask<ReviewAccess> GetReviewAccessAsync(Nothing request, CallContext context = default);

    [Requires(AccountsPermissions.ReviewAccessNode)]
    ValueTask<Done> CreateDemoAccountAsync(CreateDemoAccountRequest request, CallContext context = default);

    [Requires(AccountsPermissions.ReviewAccessNode)]
    ValueTask<CodeReply> CreateReviewCodeAsync(CreateReviewCodeRequest request, CallContext context = default);

    [Requires(AccountsPermissions.ReviewAccessNode)]
    ValueTask<Done> TurnOffReviewCodeAsync(TurnOffReviewCodeRequest request, CallContext context = default);
}
