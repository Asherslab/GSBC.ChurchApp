using GSBC.ChurchApp.Features.Chats.Contracts;
using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Features.Chats.Api.Tests;

public sealed class ChatMembershipTests
{
    private static readonly Guid Leader = Guid.NewGuid(), Helper = Guid.NewGuid(), Adult = Guid.NewGuid(), Teen = Guid.NewGuid(), Parent = Guid.NewGuid();

    [Fact]
    public void AnAudienceChatIsEveryoneInItsAudienceButNotThePaused()
    {
        MembershipPlan plan = ChatMembership.Decide(Facts([Leader, Helper, Adult], paused: [Adult]));

        Assert.Equal(new[] { Leader, Helper }.Order(), plan.Recipients.Select(participant => participant.PersonId).Order());
        Assert.Null(plan.Of(Adult));
    }

    [Fact]
    public void LeadersJoinOnlyAConversationAboutJoining()
    {
        Assert.Null(ChatMembership.Decide(Facts([Adult], leaders: [Leader])).Of(Leader));
        Assert.Equal(ChatRoles.Leader, ChatMembership.Decide(Facts([Adult], leaders: [Leader]) with { LeadersJoin = true }).Of(Leader)?.Role);
    }

    [Fact]
    public void AYoungPersonGetsNothingUntilAParentSaysYesButTheirParentReadsFromTheStart()
    {
        MembershipPlan waiting = ChatMembership.Decide(Facts([Leader, Helper, Teen]));

        Assert.Equal(ChatStates.AwaitingConsent, waiting.Of(Teen)?.State);
        Assert.DoesNotContain(waiting.Recipients, participant => participant.PersonId == Teen);
        Assert.Equal((ChatRoles.ParentReader, ChatStates.Active), (waiting.Of(Parent)?.Role, waiting.Of(Parent)?.State));
        Assert.Equal([Teen], waiting.Of(Parent)!.ForChildren);
        Assert.True(waiting.HasMinors);

        MembershipPlan approved = ChatMembership.Decide(Facts([Leader, Helper, Teen], consents: new() { [Teen] = true }));
        Assert.Contains(approved.Recipients, participant => participant.PersonId == Teen);

        MembershipPlan declined = ChatMembership.Decide(Facts([Leader, Helper, Teen], consents: new() { [Teen] = false }));
        Assert.DoesNotContain(declined.Recipients, participant => participant.PersonId == Teen);
    }

    [Fact]
    public void AChatWithYoungPeopleIsFlaggedUntilTwoAdultsInItHoldACurrentBlueCard()
    {
        Assert.StartsWith(Flagged.TooFewCardedPrefix, ChatMembership.Decide(Facts([Leader, Helper, Teen], carded: [Leader])).Flags[0]);
        Assert.StartsWith(Flagged.TooFewCardedPrefix, ChatMembership.Decide(Facts([Leader, Teen], carded: [Leader, Parent])).Flags[0]);
        Assert.Empty(ChatMembership.Decide(Facts([Leader, Helper, Teen], carded: [Leader, Helper])).Flags);
        Assert.Empty(ChatMembership.Decide(Facts([Leader, Adult], carded: [])).Flags);
    }

    [Fact]
    public void AYoungPersonWithNoParentLinkedIsFlaggedAndWaits()
    {
        MembershipFacts facts = Facts([Leader, Helper, Teen], carded: [Leader, Helper]);
        MembershipPlan plan = ChatMembership.Decide(facts with { People = new Dictionary<Guid, PersonFacts> { [Teen] = new(Teen, true, []) } });

        Assert.Equal([Flagged.NoParent(Teen)], plan.Flags);
        Assert.Equal(ChatStates.AwaitingConsent, plan.Of(Teen)?.State);
    }

    [Fact]
    public void ParentsReadButWriteOnlyForTheirChildOnceTheChildIsInAndOnlyLeadersPostInAChannel()
    {
        MembershipFacts facts = Facts([Leader, Helper, Teen], leaders: [Leader], consents: new() { [Teen] = true });
        MembershipPlan plan = ChatMembership.Decide(facts);

        Assert.False(ChatMembership.MaySend(plan, ChatKinds.Group, Parent));
        Assert.True(ChatMembership.MaySendFor(plan, ChatKinds.Group, Parent, Teen));
        Assert.False(ChatMembership.MaySendFor(ChatMembership.Decide(facts with { Consents = new Dictionary<Guid, bool?>() }), ChatKinds.Group, Parent, Teen));
        Assert.True(ChatMembership.MaySend(plan, ChatKinds.Group, Helper));
        Assert.False(ChatMembership.MaySend(plan, ChatKinds.Channel, Helper));
        Assert.True(ChatMembership.MaySend(plan, ChatKinds.Channel, Leader));
    }

    [Fact]
    public void AChatThatIsOffOrDeletedHasNobody() =>
        Assert.Empty(ChatMembership.Decide(Facts([Leader, Helper]) with { Open = false }).Participants);

    [Fact]
    public void SomeoneWhoHoldsANodeEverywhereIsNotInEveryChatButALeaderOfThisMinistryIs()
    {
        string ministry = Guid.NewGuid().ToString();
        GrantSet owner = new([new Grant(PermissionPattern.Parse("*"), Scope.Global)]);
        GrantSet ownerWhoLeads = new([.. owner.Grants, new Grant(PermissionPattern.Parse(ChatsPermissions.ManageNode), Scope.Of(ScopeKind.Ministry, ministry))]);

        Assert.False(ChatFacts.HeldThere(owner, ChatsPermissions.Manage, ministry));
        Assert.True(ChatFacts.HeldThere(ownerWhoLeads, ChatsPermissions.Manage, ministry));
    }

    private static MembershipFacts Facts(
        Guid[] audience, Guid[]? leaders = null, Guid[]? paused = null, Guid[]? carded = null, Dictionary<Guid, bool?>? consents = null) =>
        new(true, false, audience, leaders ?? [Leader],
            new Dictionary<Guid, PersonFacts> { [Teen] = new(Teen, true, [Parent]) },
            consents ?? [], (paused ?? []).ToHashSet(), (carded ?? [Leader, Helper]).ToHashSet());
}
