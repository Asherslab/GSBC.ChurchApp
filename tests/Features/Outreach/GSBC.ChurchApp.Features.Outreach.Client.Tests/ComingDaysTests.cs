using GSBC.ChurchApp.Features.Outreach.Client.Phone;
using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Client.Tests;

public sealed class ComingDaysTests
{
    private static readonly DateTime Now = new(2026, 11, 14, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void WalkersDontSeeTheDaysOfACampaignStillBeingPrepared()
    {
        Campaign preparing = new() { Id = "gympie", Stage = CampaignStage.Preparing, StageSetOn = "2026-11-14", StartsOn = "2027-02-06" };
        Campaign ongoing = new() { Id = "carols", Stage = CampaignStage.Ongoing, StageSetOn = "2026-11-07" };
        OutreachNow now = OutreachNow.Empty with
        {
            Campaigns = [preparing, ongoing],
            Days = [Day("hidden", preparing, 3), Day("shown", ongoing, 7)],
        };

        Assert.Equal(["shown"], OutreachDays.Coming(now, Now).Select(day => day.Id));
    }

    private static OutreachDay Day(string id, Campaign campaign, int daysAhead) =>
        new() { Id = id, Campaign = campaign.Id, StartsAt = Now.AddDays(daysAhead), EndsAt = Now.AddDays(daysAhead).AddHours(3) };
}
