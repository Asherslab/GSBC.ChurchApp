using GSBC.ChurchApp.Features.Ministries.Api.Data;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class MinistryArchiveTests
{
    private static readonly DateTime Monday = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AnArchivedMinistryAndEverythingUnderItLeaveTheTreeEveryScreenReads()
    {
        MinistryRecord kids = Ministry("Kids"), group = Ministry("A group", kids), class1 = Ministry("A class", group), youth = Ministry("Youth");
        MinistryTree every = new([kids, group, class1, youth]);

        foreach (MinistryRecord archiving in MinistryArchive.Archiving(every, group))
            archiving.ArchivedAt = Monday;
        MinistryTree active = new(MinistryArchive.Active([kids, group, class1, youth]));

        Assert.Equal(["Kids", "Youth"], active.All.Select(ministry => ministry.Name).Order());
        Assert.Null(active.Find(class1.Id));
        Assert.Empty(active.LifeGroups());
        Assert.Empty(active.ChildrenOf(kids.Id));
    }

    [Fact]
    public void RestoringBringsBackWhatWasArchivedWithItButNotWhatWasArchivedOnItsOwnBefore()
    {
        MinistryRecord group = Ministry("A group"), earlier = Ministry("Archived earlier", group), later = Ministry("Archived with it", group);
        earlier.ArchivedAt = Monday.AddDays(-30);
        MinistryTree every = new([group, earlier, later]);
        foreach (MinistryRecord archiving in MinistryArchive.Archiving(every, group))
            archiving.ArchivedAt = Monday;

        Assert.Equal([group.Id, later.Id], MinistryArchive.Restoring(every, group).Select(ministry => ministry.Id));
        Assert.NotNull(MinistryArchive.WhyNotRestore(every, later));
        Assert.Null(MinistryArchive.WhyNotRestore(every, group));
    }

    private static MinistryRecord Ministry(string name, MinistryRecord? under = null) =>
        new() { Id = Guid.CreateVersion7(), Name = name, ParentId = under?.Id, ShowsInLifeGroups = name == "A group" };
}
