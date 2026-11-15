using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api.Tests;

public sealed class CampaignStageRulesTests
{
    private static readonly DateOnly Starts = new(2026, 12, 1);
    private static readonly DateOnly Ends = new(2026, 12, 20);

    [Theory]
    [InlineData("2026-11-30", CampaignStage.Preparing)]
    [InlineData("2026-12-01", CampaignStage.Ongoing)]
    [InlineData("2026-12-12", CampaignStage.Ongoing)]
    [InlineData("2026-12-13", CampaignStage.WrappingUp)]
    [InlineData("2026-12-20", CampaignStage.WrappingUp)]
    [InlineData("2026-12-21", CampaignStage.Finished)]
    public void ItsDatesMoveACampaignAlong(string today, CampaignStage expected) =>
        Assert.Equal(expected, CampaignStages.Initial(Starts, Ends, 7, DateOnly.Parse(today)));

    [Fact]
    public void AMoveByHandHoldsUntilTheNextDateComes()
    {
        DateOnly movedBack = new(2026, 12, 5);

        Assert.Equal(CampaignStage.Preparing, CampaignStages.Advance(CampaignStage.Preparing, movedBack, Starts, Ends, 7, new DateOnly(2026, 12, 12)));
        Assert.Equal(CampaignStage.WrappingUp, CampaignStages.Advance(CampaignStage.Preparing, movedBack, Starts, Ends, 7, new DateOnly(2026, 12, 13)));
        Assert.Equal(CampaignStage.Ongoing, CampaignStages.Advance(CampaignStage.Ongoing, new DateOnly(2026, 12, 14), Starts, Ends, 7, new DateOnly(2026, 12, 20)));
    }

    [Theory]
    [InlineData(CampaignStage.Preparing, false)]
    [InlineData(CampaignStage.Ongoing, true)]
    [InlineData(CampaignStage.WrappingUp, true)]
    [InlineData(CampaignStage.Finished, false)]
    public void WalkersCarryOnlyCampaignsThatAreOn(CampaignStage stage, bool carried)
    {
        Campaign campaign = new() { Stage = stage, StageSetOn = "2026-11-14" };

        Assert.Equal(carried, OutreachRules.IsActive(campaign, new DateOnly(2026, 11, 14)));
        Assert.True(OutreachRules.IsActive(new Campaign { General = true, Stage = CampaignStage.Finished }, new DateOnly(2026, 11, 14)));
    }
}
