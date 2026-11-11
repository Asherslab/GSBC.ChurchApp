using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.Api.Tests;

[Integration("Closing a ticket deletes its files from Media in the same change, keeping and deleting accounts work over stored tickets")]
public sealed class ClosingTests : FeedbackHost
{
    private static readonly Guid Phone = Guid.CreateVersion7();

    [Fact]
    public async Task ResolvingATicketDeletesItsPicturesVideosAndPhoneDataButKeepsTheWords()
    {
        string id = await SendAsync(Phone, null, attachments: ["a1", "a2"], phoneData: "store");
        Assert.Equal(["a1", "a2"], Media.Uses[FeedbackMedia.AttachmentsOwner(Guid.Parse(id))]);

        Assert.Empty(Told.Settled);
        await DeskAsync(desk => desk.SetStatusAsync(Staff, new StatusChange { Id = id, Status = TicketStatus.InProgress }, null, Cancel));
        Assert.Single(Told.Settled);
        Assert.Empty(Media.Forgotten);
        await DeskAsync(desk => desk.SetStatusAsync(Staff, new StatusChange { Id = id, Status = TicketStatus.Resolved }, null, Cancel));

        Assert.Equal(["a1", "a2", "store"], Media.Forgotten.Order());
        Assert.Empty(Media.Uses[FeedbackMedia.AttachmentsOwner(Guid.Parse(id))]);
        TicketRecord ticket = Assert.IsType<TicketRecord>(await RecordAsync(id));
        Assert.Empty(ticket.Attachments);
        Assert.Null(ticket.PhoneData);
        Assert.True(ticket.AttachmentsDeleted);
        Assert.Equal("The Save button is cut off", ticket.Text);
    }

    [Fact]
    public async Task AClosedTicketIsDeletedEntirely180DaysAfterItClosed()
    {
        string id = await SendAsync(Phone, null);
        await DeskAsync(desk => desk.SetStatusAsync(Staff, new StatusChange { Id = id, Status = TicketStatus.NotDoing }, null, Cancel));

        Clock.Advance(TimeSpan.FromDays(179));
        await WithAsync(provider => provider.GetRequiredService<FeedbackKeeping>().RunAsync(Cancel));
        Assert.NotNull(await RecordAsync(id));

        Clock.Advance(TimeSpan.FromDays(1));
        await WithAsync(provider => provider.GetRequiredService<FeedbackKeeping>().RunAsync(Cancel));
        Assert.Null(await RecordAsync(id));
    }

    [Fact]
    public async Task DeletingAnAccountForgetsWhoSentItAndDeletesTheCopyOfTheirPhonesData()
    {
        Guid person = Guid.CreateVersion7();
        People.Names[person] = "Sam Sample";
        string id = await SendAsync(Phone, person, phoneData: "store");

        await WithAsync(provider => provider.GetRequiredService<FeedbackPersonData>().ForgetAsync(person, Cancel));

        TicketRecord ticket = Assert.IsType<TicketRecord>(await RecordAsync(id));
        Assert.Null(ticket.UserId);
        Assert.Equal("", ticket.SenderName);
        Assert.Null(ticket.PhoneData);
        Assert.Equal(["store"], Media.Forgotten);
    }

    [Fact]
    public async Task ThePhonesDataIsInATicketOnlyForSomeoneWhoMayReadIt()
    {
        string id = await SendAsync(Phone, null, phoneData: "store");

        Ticket triage = await WithAsync(provider => provider.GetRequiredService<FeedbackDesk>().TicketAsync(new TicketRef { Id = id }, false, Cancel));
        Ticket developer = await WithAsync(provider => provider.GetRequiredService<FeedbackDesk>().TicketAsync(new TicketRef { Id = id }, true, Cancel));

        Assert.Null(triage.PhoneData);
        Assert.True(triage.HadPhoneData);
        Assert.Equal("store", developer.PhoneData?.Id);
    }
}
