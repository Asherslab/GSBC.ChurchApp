using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Api.Tests;

public sealed class ReachTests
{
    [Fact]
    public void AnAdultIsReachedDirectlyWhateverWasChosen() =>
        Assert.Equal(Reach.Directly, Reach.Effective(Reach.ThroughParents, minor: false, canSignIn: true, hasParents: true));

    [Fact]
    public void AChildWhoCanNotSignInIsReachedThroughTheirParents() =>
        Assert.Equal(Reach.ThroughParents, Reach.Effective(Reach.Directly, minor: true, canSignIn: false, hasParents: true));

    [Fact]
    public void AYoungPersonWithNoParentLinkedIsReachedDirectly() =>
        Assert.Equal(Reach.Directly, Reach.Effective(Reach.ThroughParents, minor: true, canSignIn: true, hasParents: false));

    [Theory]
    [InlineData(Reach.Directly)]
    [InlineData(Reach.ThroughParents)]
    [InlineData(Reach.Both)]
    public void AYoungPersonWhoSignsInIsReachedAsTheirFamilyChose(string chosen) =>
        Assert.Equal(chosen, Reach.Effective(chosen, minor: true, canSignIn: true, hasParents: true));

    [Fact]
    public void UntilTheFamilyChoosesAYoungPersonWhoSignsInIsReachedBothWays() =>
        Assert.Equal(Reach.Both, Reach.Effective(null, minor: true, canSignIn: true, hasParents: true));
}
