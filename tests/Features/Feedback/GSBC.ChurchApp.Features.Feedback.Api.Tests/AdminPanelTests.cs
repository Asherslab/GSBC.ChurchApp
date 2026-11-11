using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.Api.Tests;

[Integration("The board's list filters stored tickets by where they were sent from, and an admin-panel ticket's files go from Media like a phone's")]
public sealed class AdminPanelTests : FeedbackHost
{
    [Fact]
    public async Task FeedbackFromTheAdminPanelKeepsTheBrowsersDetailsNotAPhonesAndTheBoardCanShowItOnItsOwn()
    {
        Guid person = Guid.CreateVersion7();
        Access.Held[person] = [FeedbackPermissions.SendNode];
        string fromTheApp = await SendAsync(Guid.CreateVersion7(), null);
        string fromTheAdminPanel = await SendAsync(Guid.CreateVersion7(), person, source: FeedbackSource.AdminPanel);

        DeskList admin = await WithAsync(provider => provider.GetRequiredService<FeedbackDesk>()
            .ListAsync(new DeskQuery { Filter = DeskFilter.Everything, Source = SourceFilter.AdminPanel }, Cancel));
        DeskList app = await WithAsync(provider => provider.GetRequiredService<FeedbackDesk>()
            .ListAsync(new DeskQuery { Filter = DeskFilter.Everything, Source = SourceFilter.App }, Cancel));

        Assert.Equal(fromTheAdminPanel, Assert.Single(admin.Tickets).Id);
        Assert.Equal(fromTheApp, Assert.Single(app.Tickets).Id);
        Ticket ticket = await WithAsync(provider => provider.GetRequiredService<FeedbackDesk>()
            .TicketAsync(new TicketRef { Id = fromTheAdminPanel }, false, Cancel));
        Assert.Null(ticket.Phone);
        Assert.Equal("", ticket.InstallId);
        Assert.Equal(["/media", "/"], ticket.Browser?.RecentPages);
    }

    [Fact]
    public async Task AdminPanelScreenshotsAreTheTicketsOwnFilesAndGoAWeekAfterItIsResolved()
    {
        Guid person = Guid.CreateVersion7();
        Access.Held[person] = [FeedbackPermissions.SendNode];
        string id = await SendAsync(null, person, attachments: ["shot"], phoneData: "not-from-a-phone", source: FeedbackSource.AdminPanel);
        Assert.Equal(["shot"], Media.Uses[FeedbackMedia.AttachmentsOwner(Guid.Parse(id))]);

        await DeskAsync(desk => desk.SetStatusAsync(Staff, new StatusChange { Id = id, Status = TicketStatus.Resolved }, null, Cancel));
        Clock.Advance(TimeSpan.FromDays(7));
        await WithAsync(provider => provider.GetRequiredService<FeedbackKeeping>().RunAsync(Cancel));

        Assert.Equal(["shot"], Media.Forgotten);
        Assert.False((await RecordAsync(id))?.HadPhoneData);
    }
}
