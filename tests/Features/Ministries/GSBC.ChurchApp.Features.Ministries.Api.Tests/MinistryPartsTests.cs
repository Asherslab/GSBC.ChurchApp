using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class MinistryPartsTests
{
    private readonly MinistryRecord _kids = new() { Id = Guid.CreateVersion7(), Name = "Kids Ministries", PartsFromBelow = [MinistryParts.Calendar] };
    private readonly MinistryRecord _impactKids;
    private readonly MinistryRecord _leadersNight;
    private readonly MinistryRecord _kidsLife;
    private readonly MinistryRecord _lifeGroups = new() { Id = Guid.CreateVersion7(), Name = "Life Groups" };
    private readonly MinistryRecord _faithAtWork;
    private readonly MinistryRecord _young;

    public MinistryPartsTests()
    {
        _impactKids = new() { Id = Guid.CreateVersion7(), ParentId = _kids.Id, Name = "Impact Kids" };
        _leadersNight = new() { Id = Guid.CreateVersion7(), ParentId = _impactKids.Id, Name = "Leaders' night" };
        _kidsLife = new() { Id = Guid.CreateVersion7(), ParentId = _kids.Id, Name = "Kids Life", ShowsInLifeGroups = true };
        _faithAtWork = new() { Id = Guid.CreateVersion7(), ParentId = _lifeGroups.Id, Name = "Faith at Work", ShowsInLifeGroups = true };
        _young = new() { Id = Guid.CreateVersion7(), ParentId = _lifeGroups.Id, Name = "Young Adults", ShowsInLifeGroups = true };
    }

    private MinistryTree Tree => new([_kids, _impactKids, _leadersNight, _kidsLife, _lifeGroups, _faithAtWork, _young]);

    [Fact]
    public void AMinistryCalendarCanTakeInEveryMinistryUnderItThatStillUsesItsCalendar()
    {
        Assert.Equal([_kids.Id, _impactKids.Id, _kidsLife.Id, _leadersNight.Id], MinistryViews.CalendarsShown(Tree, _kids.Id));

        _impactKids.PartsOff = [MinistryParts.Calendar];
        Assert.Equal([_kids.Id, _kidsLife.Id], MinistryViews.CalendarsShown(Tree, _kids.Id));

        _kids.PartsFromBelow = [];
        Assert.Equal([_kids.Id], MinistryViews.CalendarsShown(Tree, _kids.Id));

        _kids.PartsOff = [MinistryParts.Calendar];
        Assert.Empty(MinistryViews.CalendarsShown(Tree, _kids.Id));
    }

    [Fact]
    public void AMinistryCanShowTheNewsOfTheMinistriesUnderItSkippingAnyThatDontUseNews()
    {
        Assert.Equal([_kids.Id], MinistryViews.Gathered(Tree, _kids.Id, MinistryParts.News));

        _kids.PartsFromBelow = [MinistryParts.News];
        Assert.Equal([_kids.Id, _impactKids.Id, _kidsLife.Id, _leadersNight.Id], MinistryViews.Gathered(Tree, _kids.Id, MinistryParts.News));
        Assert.Equal([_kids.Id], MinistryViews.CalendarsShown(Tree, _kids.Id));

        _impactKids.PartsOff = [MinistryParts.News];
        Assert.Equal([_kids.Id, _kidsLife.Id], MinistryViews.Gathered(Tree, _kids.Id, MinistryParts.News));
    }

    [Fact]
    public void ATurnedOffPartShowsNothingOfItsOwnAndHidesWhatItWouldPassDown()
    {
        Assert.Equal([_leadersNight.Id, _impactKids.Id, _kids.Id], MinistryViews.Showing(Tree, _leadersNight.Id, MinistryParts.News));

        _impactKids.PartsOff = [MinistryParts.News];
        Assert.Equal([_leadersNight.Id, _kids.Id], MinistryViews.Showing(Tree, _leadersNight.Id, MinistryParts.News));
        Assert.Empty(MinistryViews.Showing(Tree, _impactKids.Id, MinistryParts.News));
        Assert.Equal([_impactKids.Id, _kids.Id], MinistryViews.Showing(Tree, _impactKids.Id, MinistryParts.Files));
    }

    [Fact]
    public void LifeGroupsListEveryMinistryShownInThemWhereverItSits()
    {
        MinistryRecord topLevel = new() { Id = Guid.CreateVersion7(), Name = "Prayer breakfast", ShowsInLifeGroups = true };
        MinistryTree tree = new([_kids, _impactKids, _leadersNight, _kidsLife, _lifeGroups, _faithAtWork, _young, topLevel]);

        Assert.Equal(new[] { _faithAtWork.Id, _kidsLife.Id, topLevel.Id, _young.Id }.Order(), tree.LifeGroups().Select(group => group.Id).Order());
        Assert.DoesNotContain(tree.LifeGroups(), group => group.Id == _lifeGroups.Id);
    }
}
