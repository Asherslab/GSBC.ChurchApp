using GSBC.ChurchApp.Features.Feedback.Api.Data;
using GSBC.ChurchApp.Features.Feedback.Contracts;

namespace GSBC.ChurchApp.Features.Feedback.Api.Tests;

public sealed class FeedbackRulesTests
{
    private static readonly Guid Phone = Guid.CreateVersion7();
    private static readonly Guid Person = Guid.CreateVersion7();

    [Fact]
    public void WithoutTheRoleOnlyAVerifiedPhoneSendsAndAtMostFiveADayWhileTheRoleSendsFifty()
    {
        Assert.Equal(SendVerdict.NotHere, FeedbackRules.MaySend(new Sender(null, Person, false, false), 0));
        Assert.Equal(SendVerdict.Allowed, FeedbackRules.MaySend(new Sender(Phone, null, false, false), 4));
        Assert.Equal(SendVerdict.TooMany, FeedbackRules.MaySend(new Sender(Phone, null, false, false), 5));
        Assert.Equal(SendVerdict.Allowed, FeedbackRules.MaySend(new Sender(null, Person, true, false), 49));
        Assert.Equal(SendVerdict.TooMany, FeedbackRules.MaySend(new Sender(null, Person, true, false), 50));
        Assert.Equal(SendVerdict.Blocked, FeedbackRules.MaySend(new Sender(Phone, Person, true, true), 0));
    }

    [Fact]
    public void FromTheAdminPanelAnyoneSignedInWithAPageThereSendsFiftyADayWithOrWithoutTheRole()
    {
        Assert.Equal(SendVerdict.NotHere, FeedbackRules.MaySendFrom(FeedbackSource.AdminPanel, new Sender(Phone, null, false, false), 0));
        Assert.Equal(SendVerdict.NotHere, FeedbackRules.MaySendFrom(FeedbackSource.AdminPanel, new Sender(null, Person, false, false), 0));
        Assert.Equal(SendVerdict.NotHere, FeedbackRules.MaySendFrom(FeedbackSource.AdminPanel, new Sender(null, Person, true, false), 0));
        Assert.Equal(SendVerdict.Allowed, FeedbackRules.MaySendFrom(FeedbackSource.AdminPanel, new Sender(null, Person, false, false, UsesTheAdminPanel: true), 49));
        Assert.Equal(SendVerdict.TooMany, FeedbackRules.MaySendFrom(FeedbackSource.AdminPanel, new Sender(null, Person, false, false, UsesTheAdminPanel: true), 50));
    }

    [Fact]
    public void ThePageFeedbackIsAboutIsTheLastOneOutsideTheMoreTabOrFailingThatTheLastOneThatIsntMoreOrFeedback()
    {
        HashSet<string> inTheMoreTab = ["/more", "/help-improve", "/feedback/send"];

        Assert.Equal("/calendar", FeedbackPages.Before(["/feedback/send", "/more", "/help-improve", "/more", "/calendar", "/"], inTheMoreTab.Contains));
        Assert.Equal("/help-improve", FeedbackPages.Before(["/feedback/send", "/more", "/help-improve", "/more"], inTheMoreTab.Contains));
        Assert.Equal("", FeedbackPages.Before(["/feedback/send", "/more"], inTheMoreTab.Contains));
    }

    [Fact]
    public void OnThePhoneTheAdminPanelCountsForNothing()
    {
        Assert.Equal(SendVerdict.NotHere, FeedbackRules.MaySendFrom(FeedbackSource.App, new Sender(null, Person, false, false), 0));
        Assert.Equal(SendVerdict.TooMany, FeedbackRules.MaySendFrom(FeedbackSource.App, new Sender(Phone, Person, false, false), 5));
        Assert.Equal(SendVerdict.Allowed, FeedbackRules.MaySendFrom(FeedbackSource.App, new Sender(null, Person, true, false), 49));
    }

    [Fact]
    public void AClosedTicketIsDeletedOnlyOnceItHasBeenClosedFor180Days()
    {
        DateTime now = new(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc);
        TicketRecord resolved = new() { Status = TicketStatus.Resolved, ClosedAt = now.AddDays(-180) };
        TicketRecord recentlyClosed = new() { Status = TicketStatus.NotDoing, ClosedAt = now.AddDays(-179) };
        TicketRecord reopened = new() { Status = TicketStatus.InProgress, ClosedAt = now.AddDays(-400) };

        Assert.True(FeedbackRules.IsDueForDeletion(resolved, now));
        Assert.False(FeedbackRules.IsDueForDeletion(recentlyClosed, now));
        Assert.False(FeedbackRules.IsDueForDeletion(reopened, now));
    }
}
