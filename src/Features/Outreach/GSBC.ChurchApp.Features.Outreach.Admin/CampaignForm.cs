using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public sealed class CampaignForm
{
    private const int LargestBundle = 1000;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public string Name { get; set; } = "";
    public string HandedOut { get; set; } = "";
    public string BundleSize { get; set; } = "";
    public string Starts { get; set; } = "";
    public string Ends { get; set; } = "";
    public string WrapUpDays { get; set; } = "";
    public string BundlesMade { get; set; } = "";
    public bool Leaderboard { get; set; }

    public static CampaignForm Of(Campaign campaign, OutreachSettings settings) => new()
    {
        Name = campaign.Name,
        HandedOut = campaign.HandedOut,
        BundleSize = Whole(CampaignStages.BundleSizeOf(campaign, settings)),
        Starts = campaign.StartsOn,
        Ends = campaign.EndsOn,
        WrapUpDays = Whole(CampaignStages.WrapUpDaysOf(campaign)),
        BundlesMade = campaign.BundlesMade > 0 ? Whole(campaign.BundlesMade) : "",
        Leaderboard = campaign.Leaderboard,
    };

    public bool Same(CampaignForm other) =>
        (Name, HandedOut, BundleSize.Trim(), Starts, Ends, WrapUpDays.Trim(), BundlesMade.Trim(), Leaderboard)
        == (other.Name, other.HandedOut, other.BundleSize.Trim(), other.Starts, other.Ends, other.WrapUpDays.Trim(), other.BundlesMade.Trim(), other.Leaderboard);

    public string? BundleSizeProblem => BundleSize.Trim().Length > 0 && Int(BundleSize) is not (>= 1 and <= LargestBundle) ? $"Give a number from 1 to {LargestBundle:N0}." : null;

    public string? WrapUpProblem =>
        Int(WrapUpDays) is not (>= 1 and <= CampaignStages.LongestWrapUpDays) ? $"Give a number of days from 1 to {CampaignStages.LongestWrapUpDays}." : null;

    public string? BundlesMadeProblem =>
        BundlesMade.Trim().Length > 0 && Int(BundlesMade) is not (>= 0 and <= CampaignStages.MostBundlesMade) ? "Give a whole number, or leave it empty." : null;

    public string? Blocked =>
        Name.Trim().Length == 0 ? "Give it a name to save it."
        : OutreachRules.DateOf(Starts) is { } starts && OutreachRules.DateOf(Ends) is { } ends && ends < starts ? "It ends on or after the day it starts."
        : BundleSizeProblem ?? WrapUpProblem ?? BundlesMadeProblem;

    public DateOnly? WrapsUpOn() =>
        Int(WrapUpDays) is { } days ? CampaignStages.WrapsUpOn(OutreachRules.DateOf(Ends), days) : null;

    public Campaign Into(Campaign from, string id) => new()
    {
        Id = id, Name = Name.Trim(), HandedOut = HandedOut.Trim(), StartsOn = Starts, EndsOn = Ends, Leaderboard = Leaderboard, General = from.General,
        Area = from.Area, Regions = from.Regions, Stage = from.Stage, StageSetOn = from.StageSetOn, ToldEndingOn = from.ToldEndingOn,
        BundleSize = Int(BundleSize) ?? 0, WrapUpDays = Int(WrapUpDays) ?? CampaignStages.DefaultWrapUpDays, BundlesMade = Int(BundlesMade) ?? 0,
    };

    private static string Whole(int value) => value.ToString(Invariant);

    private static int? Int(string text) => int.TryParse(text.Trim(), NumberStyles.Integer, Invariant, out int value) ? value : null;
}
