using Grpc.Core;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.Api.Tests;

[Integration("Counting a phone's feedback over the last day limits what one install or one role holder sends, a block refuses a phone, and a resent ticket is stored once")]
public sealed class SendingTests : FeedbackHost
{
    private static readonly Guid Phone = Guid.CreateVersion7();

    [Fact]
    public async Task AVerifiedPhoneSendsFiveADayAndNoMore()
    {
        for (int sent = 0; sent < 5; sent++)
            await SendAsync(Phone, null);

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => SendAsync(Phone, null));
        Assert.Equal(StatusCode.ResourceExhausted, refused.StatusCode);
        Clock.Advance(TimeSpan.FromDays(1));
        await SendAsync(Phone, null);
    }

    [Fact]
    public async Task SomeoneWithoutTheRoleCannotSendFromAPhoneThatHasntProvedItself()
    {
        Guid person = Guid.CreateVersion7();
        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => SendAsync(null, person));
        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);

        Access.Held[person] = [FeedbackPermissions.SendNode];
        for (int sent = 0; sent < 6; sent++)
            await SendAsync(null, person);
    }

    [Fact]
    public async Task ABlockedPhoneCannotSendOrUploadUntilItIsUnblocked()
    {
        string first = await SendAsync(Phone, null);
        await DeskAsync(desk => desk.BlockAsync(Staff, new BlockSave { TicketId = first, Reason = "Sending rubbish" }, Cancel));

        RpcException refused = await Assert.ThrowsAsync<RpcException>(() => SendAsync(Phone, null));
        Assert.Equal(StatusCode.PermissionDenied, refused.StatusCode);
        Assert.False(await WithAsync(provider => provider.GetRequiredService<FeedbackSenders>().MayUploadFromInstallAsync(Phone, Cancel)));

        await DeskAsync(desk => desk.UnblockAsync(new PhoneRef { InstallId = Phone.ToString() }, Cancel));
        Assert.True(await WithAsync(provider => provider.GetRequiredService<FeedbackSenders>().MayUploadFromInstallAsync(Phone, Cancel)));
        await SendAsync(Phone, null);
    }

    [Fact]
    public async Task TheOutboxSendingATicketAgainStoresItOnceAndTellsTriageOnce()
    {
        string id = Guid.NewGuid().ToString();
        await SendAsync(Phone, null, id: id);
        await SendAsync(Phone, null, id: id);

        Assert.Single(Told.Raised);
        Assert.NotNull(await RecordAsync(id));
    }
}
