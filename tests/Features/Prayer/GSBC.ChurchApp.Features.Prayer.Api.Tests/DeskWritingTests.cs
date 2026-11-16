using Grpc.Core;
using GSBC.ChurchApp.Features.Prayer.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Prayer.Api.Tests;

[Integration("Prayer wall staff write requests at the desk straight onto the wall, under the church's name or for someone else; only those are theirs to delete, and only they may send one from the church")]
public sealed class DeskWritingTests : PrayerHost
{
    private static readonly Guid Phone = Guid.CreateVersion7();

    [Fact]
    public async Task ARequestFromTheChurchGoesStraightOnTheWallUnderTheChurchsNameAndFollowsARename()
    {
        string id = await WriteAsync(WrittenFrom.Church);

        Assert.Equal("GSBC", Assert.Single(await WallAsync()).Name);
        Assert.Null((await RecordAsync(id))!.UserId);

        await WithAsync(provider => provider.GetRequiredService<ChurchName>().RenameAsync("Good Shepherd", Cancel));

        Assert.Equal("Good Shepherd", Assert.Single(await WallAsync()).Name);
        Assert.Equal("Good Shepherd", PrayerPayloads.Decode<PrayerChurch>(Assert.Single(await LiveRowsAsync(PrayerFeeds.Church)).Payload).Name);
    }

    [Fact]
    public async Task SomeoneElsesRequestShowsTheirShortNameAndIsNotTheWritersOwn()
    {
        string id = await WriteAsync(WrittenFrom.SomeoneElse, "Jo Sample");

        Assert.Equal("Jo S.", Assert.Single(await WallAsync()).Name);
        Assert.Null((await RecordAsync(id))!.UserId);
        Assert.Empty(await LiveRowsAsync(PrayerFeeds.Mine));
    }

    [Fact]
    public async Task PrayerWallStaffDeleteWhatWasWrittenForTheChurchOrSomeoneElseButNotWhatSomeoneSent()
    {
        string church = await WriteAsync(WrittenFrom.Church);
        string sent = await SendAsync(Phone, null, "For my exams", forWall: true);

        await StaffAsync(care => care.DeleteAsync(mayRead: false, new RequestRef { Id = church }, Cancel));
        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => StaffAsync(care => care.DeleteAsync(mayRead: false, new RequestRef { Id = sent }, Cancel)));

        Assert.Null(await RecordAsync(church));
        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);
        Assert.NotNull(await RecordAsync(sent));
    }

    [Fact]
    public async Task OnlyPrayerWallStaffSendARequestFromTheChurchFromTheirPhone()
    {
        Guid member = Guid.CreateVersion7();
        PrayerRequestSend send = new() { Id = Guid.NewGuid().ToString(), Text = "For the church camp", ForWall = true, FromChurch = true };

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => AppAsync(app => app.SendAsync(Phone, member, send, false, Cancel)));
        await AppAsync(app => app.SendAsync(Phone, member, send, true, Cancel));

        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);
        Assert.Equal(("GSBC", true, (Guid?)null), ((await RecordAsync(send.Id))!.Name, (await RecordAsync(send.Id))!.WallShowsName, (await RecordAsync(send.Id))!.UserId));
    }

    private async Task<string> WriteAsync(WrittenFrom from, string name = "")
    {
        string id = Guid.NewGuid().ToString();
        await StaffAsync(care => care.WriteAsync(Staff, new PastoralWrite
        {
            Id = id, Text = "For our missionaries", From = from, Name = name, ForWall = true, Until = Clock.GetUtcNow().UtcDateTime.AddDays(30),
        }, Cancel));
        return id;
    }
}
