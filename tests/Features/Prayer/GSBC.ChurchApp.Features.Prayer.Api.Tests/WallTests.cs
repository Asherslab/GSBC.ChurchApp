using Grpc.Core;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Api.Tests;

[Integration("A request reaches the public wall feed only once staff show it, never with a name the person didn't allow, and leaves it when its time is up or the person takes it down")]
public sealed class WallTests : PrayerHost
{
    private static readonly Guid Phone = Guid.CreateVersion7();

    [Fact]
    public async Task NothingReachesTheWallUntilStaffShowIt()
    {
        string id = await SendAsync(Phone, null, "For my exams", forWall: true);
        Assert.Empty(await RowsAsync(PrayerFeeds.Wall));

        await ShowAsync(id);

        WallPost post = Assert.Single(await WallAsync());
        Assert.Equal("Please pray for us.", post.Text);
    }

    [Fact]
    public async Task ARequestThatDidNotAskForTheWallIsNeverShown()
    {
        string id = await SendAsync(Phone, null, "Just for the pastors");

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => ShowAsync(id));

        Assert.Equal(StatusCode.FailedPrecondition, refused.StatusCode);
        Assert.Empty(await RowsAsync(PrayerFeeds.Wall));
    }

    [Fact]
    public async Task ANameTheyDidNotAllowNeverReachesTheWall()
    {
        Guid person = Guid.CreateVersion7();
        People.Names[person] = "Sam Sample";
        string hidden = await SendAsync(Phone, person, "No name, please", forWall: true, showName: false);
        string shown = await SendAsync(Phone, person, "Use my name", forWall: true, showName: true);

        await ShowAsync(hidden, showName: true);
        await ShowAsync(shown, showName: true);

        List<WallPost> wall = await WallAsync();
        Assert.Equal("", wall.Single(post => post.Id == hidden).Name);
        Assert.Equal("Sam S.", wall.Single(post => post.Id == shown).Name);
        Assert.All(await RowsAsync(PrayerFeeds.Wall), row => Assert.Null(row.ScopeId));
    }

    [Fact]
    public async Task AChildsRequestReachesTheWallOnlyOnceApprovedAndTheApproverSeesTheyAreUnder18()
    {
        Guid child = Guid.CreateVersion7();
        People.Ages[child] = 13;
        string id = await SendAsync(Phone, child, "For my grandma", forWall: true);

        RequestDetails details = await WithAsync(provider => Get(provider, id));
        Assert.True(details.Line.Minor);
        Assert.Empty(await RowsAsync(PrayerFeeds.Wall));

        await ShowAsync(id);
        Assert.Single(await WallAsync());
    }

    [Fact]
    public async Task ItLeavesTheWallWhenItsTimeIsUp()
    {
        string id = await SendAsync(Phone, null, "For rain", forWall: true);
        await ShowAsync(id);

        Clock.Advance(TimeSpan.FromDays(30) + TimeSpan.FromMinutes(1));
        await WithAsync(provider => provider.GetRequiredService<PrayerKeeping>().RunAsync(Cancel));

        Assert.Empty(await WallAsync());
        Assert.Equal(WallState.Ended, (await RecordAsync(id))!.Wall);
    }

    [Fact]
    public async Task ThePersonCanTakeItDownAndNobodyElseCan()
    {
        string id = await SendAsync(Phone, null, "For our street", forWall: true);
        await ShowAsync(id);

        await AppAsync(app => app.TakeDownAsync(Guid.CreateVersion7(), null, new RequestRef { Id = id }, Cancel));
        Assert.Single(await WallAsync());

        await AppAsync(app => app.TakeDownAsync(Phone, null, new RequestRef { Id = id }, Cancel));
        Assert.Empty(await WallAsync());
        Assert.Equal(WallState.TakenDown, (await RecordAsync(id))!.Wall);
    }

    private static Task<RequestDetails> Get(IServiceProvider provider, string id) =>
        provider.GetRequiredService<PastoralCare>().RequestAsync(mayRead: false, id, Cancel);
}
