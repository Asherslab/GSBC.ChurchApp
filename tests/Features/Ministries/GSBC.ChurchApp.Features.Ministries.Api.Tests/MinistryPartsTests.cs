using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class MinistryPartsTests
{
    private readonly MinistryRecord _kids = new() { Id = Guid.CreateVersion7(), Name = "Kids Ministries", Kind = MinistryKinds.Ministry, CalendarsFromBelow = true };
    private readonly MinistryRecord _impactKids;
    private readonly MinistryRecord _leadersNight;
    private readonly MinistryRecord _kidsLife;
    private readonly MinistryRecord _lifeGroups = new() { Id = Guid.CreateVersion7(), Name = "Life Groups", Kind = MinistryKinds.Ministry };
    private readonly MinistryRecord _faithAtWork;
    private readonly MinistryRecord _young;

    public MinistryPartsTests()
    {
        _impactKids = new() { Id = Guid.CreateVersion7(), ParentId = _kids.Id, Name = "Impact Kids", Kind = MinistryKinds.Ministry };
        _leadersNight = new() { Id = Guid.CreateVersion7(), ParentId = _impactKids.Id, Name = "Leaders' night", Kind = MinistryKinds.Ministry };
        _kidsLife = new() { Id = Guid.CreateVersion7(), ParentId = _kids.Id, Name = "Kids Life", Kind = MinistryKinds.LifeGroup };
        _faithAtWork = new() { Id = Guid.CreateVersion7(), ParentId = _lifeGroups.Id, Name = "Faith at Work", Kind = MinistryKinds.LifeGroup };
        _young = new() { Id = Guid.CreateVersion7(), ParentId = _lifeGroups.Id, Name = "Young Adults", Kind = MinistryKinds.LifeGroup };
    }

    private MinistryTree Tree => new([_kids, _impactKids, _leadersNight, _kidsLife, _lifeGroups, _faithAtWork, _young]);

    [Fact]
    public void AMinistryCalendarCanTakeInEveryMinistryUnderItThatStillUsesItsCalendar()
    {
        Assert.Equal([_kids.Id, _impactKids.Id, _kidsLife.Id, _leadersNight.Id], MinistryViews.CalendarsShown(Tree, _kids.Id));

        _impactKids.PartsOff = [MinistryParts.Calendar];
        Assert.Equal([_kids.Id, _kidsLife.Id], MinistryViews.CalendarsShown(Tree, _kids.Id));

        _kids.CalendarsFromBelow = false;
        Assert.Equal([_kids.Id], MinistryViews.CalendarsShown(Tree, _kids.Id));

        _kids.PartsOff = [MinistryParts.Calendar];
        Assert.Empty(MinistryViews.CalendarsShown(Tree, _kids.Id));
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
    public void LifeGroupsLiveWhereTheirSundaysAreSetUpEvenWhenOneSitsUnderAnotherMinistry()
    {
        Assert.Equal(_lifeGroups.Id, Tree.LifeGroupsHome([]));
        Assert.Equal(_lifeGroups.Id, Tree.LifeGroupsHome([Guid.CreateVersion7(), _lifeGroups.Id]));

        MinistryTree onlyKidsLife = new([_kids, _impactKids, _kidsLife, _lifeGroups]);
        Assert.Equal(_lifeGroups.Id, onlyKidsLife.LifeGroupsHome([_lifeGroups.Id]));
        Assert.Null(onlyKidsLife.LifeGroupsHome([]));
    }
}
