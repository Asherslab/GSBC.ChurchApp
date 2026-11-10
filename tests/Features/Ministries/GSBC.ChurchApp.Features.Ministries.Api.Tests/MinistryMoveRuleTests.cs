using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class MinistryMoveRuleTests
{
    private readonly MinistryRecord _music = new() { Id = Guid.CreateVersion7(), Name = "Music", PartsFromBelow = [MinistryParts.Calendar] };
    private readonly MinistryRecord _choir;
    private readonly MinistryRecord _altos;
    private readonly MinistryRecord _kids = new() { Id = Guid.CreateVersion7(), Name = "Kids Ministries", PartsFromBelow = [MinistryParts.News] };
    private readonly MinistryRecord _kidsLife;

    public MinistryMoveRuleTests()
    {
        _choir = new() { Id = Guid.CreateVersion7(), ParentId = _music.Id, Name = "Choir" };
        _altos = new() { Id = Guid.CreateVersion7(), ParentId = _choir.Id, Name = "Altos" };
        _kidsLife = new() { Id = Guid.CreateVersion7(), ParentId = _kids.Id, Name = "Kids Life", ShowsInLifeGroups = true };
    }

    private MinistryTree Tree => new([_music, _choir, _altos, _kids, _kidsLife]);

    [Fact]
    public void AMinistryNeverMovesUnderItselfOrAnythingUnderItAndOneShownInLifeGroupsMovesLikeAnyOther()
    {
        static bool Anywhere(MinistryRecord? place) => true;

        Assert.NotNull(MinistryRules.WhyNotMove(Tree, _choir, _choir, Anywhere));
        Assert.NotNull(MinistryRules.WhyNotMove(Tree, _music, _altos, Anywhere));
        Assert.NotNull(MinistryRules.WhyNotMove(Tree, _choir, _music, Anywhere));
        Assert.Null(MinistryRules.WhyNotMove(Tree, _choir, _kidsLife, Anywhere));
        Assert.Null(MinistryRules.WhyNotMove(Tree, _kidsLife, null, Anywhere));
        Assert.Null(MinistryRules.WhyNotMove(Tree, _choir, _kids, Anywhere));
        Assert.Null(MinistryRules.WhyNotMove(Tree, _kidsLife, _music, Anywhere));
    }

    [Fact]
    public void TheMoverMustBeAllowedToAddUnderTheNewPlaceAndOnlyStaffMakeAMinistryTopLevel()
    {
        bool LeadsKids(MinistryRecord? place) => place?.Id == _kids.Id;

        Assert.Null(MinistryRules.WhyNotMove(Tree, _choir, _kids, LeadsKids));
        Assert.NotNull(MinistryRules.WhyNotMove(Tree, _altos, _music, LeadsKids));
        Assert.NotNull(MinistryRules.WhyNotMove(Tree, _choir, null, LeadsKids));
        Assert.Null(MinistryRules.WhyNotMove(Tree, _choir, null, place => place is null));
    }

    [Fact]
    public void MovingChangesWhichMinistriesItsLeadersComeFromAndWhichCombinedNewsAndCalendarsItIsIn()
    {
        MoveEffects effects = MoveEffects.Of(Tree, _choir, _kids);

        Assert.Equal([_music.Id], effects.Leaving.Select(place => place.Id));
        Assert.Equal([_kids.Id], effects.Joining.Select(place => place.Id));
        Assert.Equal(["Music's calendar"], effects.LeavesGathering);
        Assert.Equal(["Kids Ministries' news"], effects.JoinsGathering);

        MoveEffects upOne = MoveEffects.Of(Tree, _altos, _music);
        Assert.Equal([_choir.Id], upOne.Leaving.Select(place => place.Id));
        Assert.Empty(upOne.Joining);
        Assert.Equal([_music.Id], upOne.Staying);
        Assert.Empty(upOne.LeavesGathering);
    }
}
