using Grpc.Core;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Testing;

namespace GSBC.ChurchApp.Features.Prayer.Api.Tests;

[Integration("Counting a phone's requests and prayers over the last day limits what one install can send, and a sent request is stored once however often the outbox sends it")]
public sealed class LimitTests : PrayerHost
{
    private static readonly Guid Phone = Guid.CreateVersion7();

    [Fact]
    public async Task APhoneSendsAtMostFiveRequestsADay()
    {
        for (int sent = 0; sent < 5; sent++)
            await SendAsync(Phone, null, $"Request {sent}");

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => SendAsync(Phone, null, "One more"));
        Assert.Equal(StatusCode.ResourceExhausted, refused.StatusCode);
        await SendAsync(Guid.CreateVersion7(), null, "Another phone");

        Clock.Advance(TimeSpan.FromDays(1));
        await SendAsync(Phone, null, "Tomorrow");
    }

    [Fact]
    public async Task SendingTheSameRequestAgainStoresItOnce()
    {
        PrayerRequestSend send = new() { Id = Guid.NewGuid().ToString(), Text = "For peace" };

        await AppAsync(app => app.SendAsync(Phone, null, send, Cancel));
        await AppAsync(app => app.SendAsync(Phone, null, send, Cancel));

        Assert.Single(Told.Raised);
        Assert.NotNull(await RecordAsync(send.Id));
    }

    [Fact]
    public async Task APhonePraysOnceForARequestAndCanTakeItBack()
    {
        string id = await SendAsync(Guid.CreateVersion7(), null, "For our church", forWall: true);
        await ShowAsync(id);

        await AppAsync(app => app.PrayAsync(Phone, null, new PrayMark { Id = id, Prayed = true }, Cancel));
        await AppAsync(app => app.PrayAsync(Phone, null, new PrayMark { Id = id, Prayed = true }, Cancel));
        await AppAsync(app => app.PrayAsync(Guid.CreateVersion7(), null, new PrayMark { Id = id, Prayed = true }, Cancel));
        Assert.Equal(2, Assert.Single(await WallAsync()).Prayed);

        await AppAsync(app => app.PrayAsync(Phone, null, new PrayMark { Id = id, Prayed = false }, Cancel));
        Assert.Equal(1, Assert.Single(await WallAsync()).Prayed);
    }

    [Fact]
    public async Task EachPrayerGoesIntoItsRequestsDigestCountedFromWhenItWentOnTheWall()
    {
        Guid person = Guid.CreateVersion7(), friend = Guid.CreateVersion7();
        string id = await SendAsync(Guid.CreateVersion7(), person, "For my job interview", forWall: true);
        Clock.Advance(TimeSpan.FromHours(2));
        await ShowAsync(id);
        DateTime shown = Clock.UtcNow;

        await AppAsync(app => app.PrayAsync(Guid.CreateVersion7(), friend, new PrayMark { Id = id, Prayed = true }, Cancel));
        await AppAsync(app => app.PrayAsync(Guid.CreateVersion7(), null, new PrayMark { Id = id, Prayed = true }, Cancel));

        Assert.Equal([(friend, shown), (null, shown)], Told.Collected.Select(item => (item.Actor, item.StartedAt)));
        Assert.All(Told.Collected, item => Assert.Equal((PrayerNotifications.PrayedFor, Guid.Parse(id).ToString("N")), (item.Kind, item.Group)));
    }
}
