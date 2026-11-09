using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;
using GSBC.ChurchApp.Platform.Api.People;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

public sealed class FamilyRulesTests
{
    private static readonly Guid Parent = Guid.CreateVersion7();
    private static readonly Guid OtherParent = Guid.CreateVersion7();
    private static readonly Guid Child = Guid.CreateVersion7();
    private static readonly Guid Stranger = Guid.CreateVersion7();
    private static readonly FamilyLink Link = new() { ParentId = Parent, ChildId = Child, How = LinkHow.Added };

    [Fact]
    public void AYoungPersonCanNotUnlinkTheirParentButAnAdultChildCan()
    {
        Assert.NotNull(FamilyRules.WhyNotUnlink(Child, Link, childIsMinor: true, callerLinksFamilies: false));
        Assert.Null(FamilyRules.WhyNotUnlink(Child, Link, childIsMinor: false, callerLinksFamilies: false));
    }

    [Fact]
    public void TheParentOrTheOfficeCanUnlinkButNoOneElse()
    {
        Assert.Null(FamilyRules.WhyNotUnlink(Parent, Link, childIsMinor: true, callerLinksFamilies: false));
        Assert.Null(FamilyRules.WhyNotUnlink(Stranger, Link, childIsMinor: true, callerLinksFamilies: true));
        Assert.NotNull(FamilyRules.WhyNotUnlink(OtherParent, Link, childIsMinor: true, callerLinksFamilies: false));
    }

    [Fact]
    public void NoOneIsTheirOwnParentOrTheirChildsChild()
    {
        Assert.NotNull(FamilyRules.WhyNotLink(Parent, Parent, []));
        Assert.NotNull(FamilyRules.WhyNotLink(Child, Parent, [Link]));
        Assert.Null(FamilyRules.WhyNotLink(OtherParent, Child, [Link]));
    }

    [Fact]
    public void AChildWhoCanNotSignInIsLeftAloneOnlyWhenTheirLastParentGoes()
    {
        Dictionary<Guid, PersonAccount> accounts = new() { [Child] = new PersonAccount(Child, "Mia", CanSignIn: false, AddedByFamily: true, Deleted: false) };

        Assert.Equal([Child], FamilyRules.LeftAlone(Parent, [Link], accounts));
        Assert.Empty(FamilyRules.LeftAlone(Parent, [Link, new FamilyLink { ParentId = OtherParent, ChildId = Child, How = LinkHow.Office }], accounts));
    }

    [Fact]
    public void AChildWithTheirOwnSignInKeepsTheirAccountWhenTheirParentGoes()
    {
        Dictionary<Guid, PersonAccount> accounts = new() { [Child] = new PersonAccount(Child, "Leo", CanSignIn: true, AddedByFamily: true, Deleted: false) };

        Assert.Empty(FamilyRules.LeftAlone(Parent, [Link], accounts));
    }

    [Fact]
    public void SomeoneSetsTheirOwnBirthDateOnceAndTheOfficeChangesItAfter()
    {
        Assert.True(FamilyRules.MaySetOwnBirthDate(null));
        Assert.True(FamilyRules.MaySetOwnBirthDate(new PersonRecord { SaysAdult = true }));
        Assert.False(FamilyRules.MaySetOwnBirthDate(new PersonRecord { BirthDate = new DateOnly(2010, 5, 1) }));
    }
}
