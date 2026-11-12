using GSBC.ChurchApp.Features.Ministries.Client;
using GSBC.ChurchApp.Features.Ministries.Contracts;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

public sealed class HeldPartsTests
{
    private const string Id = "kids";
    private static readonly string[] Stored = [MinistryParts.News, MinistryParts.Calendar, MinistryParts.Files, MinistryParts.Rosters, MinistryParts.Chats];
    private static readonly string[] Moved = [MinistryParts.Calendar, MinistryParts.News, MinistryParts.Files, MinistryParts.Rosters, MinistryParts.Chats];

    [Fact]
    public void AQueuedOrderShowsUntilThePhonesStoreHasIt()
    {
        HeldParts held = new();
        held.Over(Ministry(Stored));
        held.Hold(new PartsSave { MinistryId = Id, PartsOrder = [.. Moved] });

        Assert.Equal(Moved, held.Over(Ministry(Stored)).PartsOrder);
        Assert.Equal(Moved, held.Over(Ministry(Moved)).PartsOrder);
        Assert.Equal(Stored, held.Over(Ministry(Stored)).PartsOrder);
    }

    [Fact]
    public void SomeoneElsesChangeWinsOverAQueuedOne()
    {
        HeldParts held = new();
        held.Over(Ministry(Stored));
        held.Hold(new PartsSave { MinistryId = Id, PartsOrder = [.. Moved] });
        string[] theirs = [MinistryParts.Chats, MinistryParts.News, MinistryParts.Calendar, MinistryParts.Files, MinistryParts.Rosters];

        Assert.Equal(theirs, held.Over(Ministry(theirs)).PartsOrder);
    }

    [Fact]
    public void TwoQueuedChangesShowTheLaterUntilBothArrive()
    {
        HeldParts held = new();
        held.Over(Ministry(Stored));
        held.Hold(new PartsSave { MinistryId = Id, PartsOff = [MinistryParts.Rosters] });
        held.Hold(new PartsSave { MinistryId = Id, PartsOff = [MinistryParts.Rosters], PartsOrder = [.. Moved] });

        MinistryDetails halfway = held.Over(Ministry(Stored, off: [MinistryParts.Rosters]));

        Assert.Equal(Moved, halfway.PartsOrder);
        Assert.Equal([MinistryParts.Rosters], halfway.PartsOff);
    }

    private static MinistryDetails Ministry(string[] order, List<string>? off = null) =>
        new() { Id = Id, PartsOrder = [.. order], PartsOff = off ?? [], ChatStarters = ChatStarters.Leaders };
}
