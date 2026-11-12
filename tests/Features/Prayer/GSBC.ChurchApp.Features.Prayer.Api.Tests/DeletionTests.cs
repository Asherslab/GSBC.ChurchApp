using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Api.Tests;

[Integration("Deleting a request, an account, or a request kept a year removes its rows and its prayers and takes it out of every feed")]
public sealed class DeletionTests : PrayerHost
{
    private static readonly Guid Phone = Guid.CreateVersion7();

    [Fact]
    public async Task ThePersonCanDeleteTheirRequestAndNobodyElseCan()
    {
        string id = await SendAsync(Phone, null, "For our family", forWall: true);
        await ShowAsync(id);

        await AppAsync(app => app.DeleteAsync(Guid.CreateVersion7(), null, new RequestRef { Id = id }, Cancel));
        Assert.NotNull(await RecordAsync(id));

        await AppAsync(app => app.DeleteAsync(Phone, null, new RequestRef { Id = id }, Cancel));
        Assert.Null(await RecordAsync(id));
        Assert.Empty(await WallAsync());
        Assert.Empty(await LiveRowsAsync(PrayerFeeds.Inbox));
        Assert.Equal([Guid.Parse(id).ToString("N")], Told.Forgotten);
    }

    [Fact]
    public async Task DeletingAnAccountForgetsTheirRequestsAndTheirPrayers()
    {
        Guid person = Guid.CreateVersion7();
        string theirs = await SendAsync(Phone, person, "Mine", forWall: true);
        string someoneElses = await SendAsync(Guid.CreateVersion7(), null, "Another", forWall: true);
        await ShowAsync(theirs);
        await ShowAsync(someoneElses);
        await AppAsync(app => app.PrayAsync(Phone, person, new PrayMark { Id = someoneElses, Prayed = true }, Cancel));

        await WithAsync(provider => provider.GetRequiredService<PrayerPersonData>().ForgetAsync(person, Cancel));

        Assert.Null(await RecordAsync(theirs));
        Assert.Empty(await LiveRowsAsync(PrayerFeeds.Mine));
        WallPost left = Assert.Single(await WallAsync());
        Assert.Equal(someoneElses, left.Id);
        Assert.Equal(0, left.Prayed);
    }

    [Fact]
    public async Task ARequestIsDeletedAYearAfterItLastChanged()
    {
        string old = await SendAsync(Phone, null, "Last year");
        Clock.Advance(TimeSpan.FromDays(200));
        string recent = await SendAsync(Phone, null, "This year");
        Clock.Advance(TimeSpan.FromDays(166));

        await WithAsync(provider => provider.GetRequiredService<PrayerKeeping>().RunAsync(Cancel));

        Assert.Null(await RecordAsync(old));
        Assert.NotNull(await RecordAsync(recent));
    }
}
