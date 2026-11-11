using GSBC.ChurchApp.Features.Feedback.Contracts;
using GSBC.ChurchApp.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Feedback.Api.Tests;

[Integration("The board's list filters tickets by where they were sent from")]
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
}
