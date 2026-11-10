using Grpc.Core;
using GSBC.ChurchApp.Features.Accounts.Contracts;
using GSBC.ChurchApp.Platform.Client.Online;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Accounts.Client;

[AdminOnly]
public sealed class AccountsOfficeClient(ChannelBase channel)
{
    private readonly IAccountsService _accounts = channel.CreateGrpcService<IAccountsService>();

    public ValueTask<PinnedPages> GetPinnedPagesAsync() => _accounts.GetPinnedPagesAsync(new Nothing());

    public ValueTask<Done> SetPinnedPagesAsync(IReadOnlyList<string> hrefs) => _accounts.SetPinnedPagesAsync(new PinnedPages { Hrefs = [.. hrefs] });

    public ValueTask<ToolConnection> ConnectToolAsync(string name) => _accounts.ConnectToolAsync(new ConnectToolRequest { Name = name });

    public ValueTask<PeopleList> SearchPeopleAsync(string query) => _accounts.SearchPeopleAsync(new SearchPeopleRequest { Query = query });

    public ValueTask<PeopleList> GetPeopleAsync(IEnumerable<string> ids) => _accounts.GetPeopleAsync(new GetPeopleRequest { Ids = ids.ToList() });

    public ValueTask<Done> InviteAsync(string name, string email) => _accounts.InviteAsync(new InviteRequest { DisplayName = name, Email = email });

    public ValueTask<AddedPerson> AddPersonAsync(string name, string email) => _accounts.AddPersonAsync(new InviteRequest { DisplayName = name, Email = email });

    public ValueTask<SentInvitations> SendHeldInvitationsAsync(int expected) =>
        _accounts.SendHeldInvitationsAsync(new SendHeldInvitationsRequest { Expected = expected });

    public ValueTask<InvitationList> ListInvitationsAsync() => _accounts.ListInvitationsAsync(new Nothing());

    public ValueTask<Done> ResendInvitationAsync(string userId) => _accounts.ResendInvitationAsync(new InvitationRequest { UserId = userId });

    public ValueTask<Done> WithdrawInvitationAsync(string userId) => _accounts.WithdrawInvitationAsync(new InvitationRequest { UserId = userId });

    public ValueTask<CodeReply> IssueRecoveryCodeAsync(string userId) => _accounts.IssueRecoveryCodeAsync(new IssueRecoveryCodeRequest { UserId = userId });

    public ValueTask<ReviewAccess> GetReviewAccessAsync() => _accounts.GetReviewAccessAsync(new Nothing());

    public ValueTask<Done> CreateDemoAccountAsync(string name, string email) =>
        _accounts.CreateDemoAccountAsync(new CreateDemoAccountRequest { DisplayName = name, Email = email });

    public ValueTask<CodeReply> CreateReviewCodeAsync(string userId, string label, int days) =>
        _accounts.CreateReviewCodeAsync(new CreateReviewCodeRequest { UserId = userId, Label = label, Days = days });

    public ValueTask<Done> TurnOffReviewCodeAsync(string id) => _accounts.TurnOffReviewCodeAsync(new TurnOffReviewCodeRequest { Id = id });

    public ValueTask<Done> DeleteAccountAsync(string userId) => _accounts.DeleteAccountAsync(new InvitationRequest { UserId = userId });

    public ValueTask<Done> SetDisabledAsync(string userId, bool disabled) =>
        _accounts.SetDisabledAsync(new SetDisabledRequest { UserId = userId, Disabled = disabled });

    public ValueTask<AuditList> ListAuditAsync(string? userId) => _accounts.ListAuditAsync(new AuditQuery { UserId = userId });
}
