namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class CampaignStages
{
    public const int DefaultWrapUpDays = 7;
    public const int UsualBundleSize = 50;
    public const int LongestWrapUpDays = 60;
    public const int MostBundlesMade = 100_000;

    public static readonly IReadOnlyList<CampaignStage> InOrder = [CampaignStage.Preparing, CampaignStage.Ongoing, CampaignStage.WrappingUp, CampaignStage.Finished];

    private static int Rank(CampaignStage stage) => stage switch
    {
        CampaignStage.Preparing => 0,
        CampaignStage.Ongoing => 1,
        CampaignStage.WrappingUp => 2,
        _ => 3,
    };

    public static bool Carried(CampaignStage stage) => stage is CampaignStage.Ongoing or CampaignStage.WrappingUp;

    public static CampaignStage Advance(CampaignStage stage, DateOnly setOn, DateOnly? starts, DateOnly? ends, int wrapUpDays, DateOnly today)
    {
        foreach ((CampaignStage next, DateOnly on) in Thresholds(starts, ends, wrapUpDays))
            if (on > setOn && on <= today && Rank(next) > Rank(stage))
                stage = next;
        return stage;
    }

    public static CampaignStage Initial(DateOnly? starts, DateOnly? ends, int wrapUpDays, DateOnly today) =>
        Advance(CampaignStage.Preparing, DateOnly.MinValue, starts, ends, wrapUpDays, today);

    public static CampaignStage Now(Campaign campaign, DateOnly today) =>
        campaign.General
            ? CampaignStage.Ongoing
            : Advance(campaign.Stage, OutreachRules.DateOf(campaign.StageSetOn) ?? DateOnly.MinValue, OutreachRules.DateOf(campaign.StartsOn),
                OutreachRules.DateOf(campaign.EndsOn), WrapUpDaysOf(campaign), today);

    public static int WrapUpDaysOf(Campaign campaign) => campaign.WrapUpDays > 0 ? campaign.WrapUpDays : DefaultWrapUpDays;

    public static DateOnly? WrapsUpOn(DateOnly? ends, int wrapUpDays) => ends?.AddDays(-wrapUpDays);

    private static DateOnly? FinishesOn(DateOnly? ends) => ends?.AddDays(1);

    public static int BundleSizeOf(Campaign? campaign, OutreachSettings settings) =>
        campaign is { BundleSize: > 0 } ? campaign.BundleSize : settings.BundleSize;

    public static int BundlesOut(int letterboxes, int bundleSize) => bundleSize > 0 ? (int)Math.Ceiling(letterboxes / (double)bundleSize) : 0;

    private static IEnumerable<(CampaignStage Stage, DateOnly On)> Thresholds(DateOnly? starts, DateOnly? ends, int wrapUpDays)
    {
        if (starts is { } start)
            yield return (CampaignStage.Ongoing, start);
        if (WrapsUpOn(ends, wrapUpDays) is { } wrap)
            yield return (CampaignStage.WrappingUp, starts is { } begun && wrap < begun ? begun : wrap);
        if (FinishesOn(ends) is { } finish)
            yield return (CampaignStage.Finished, finish);
    }
}
