using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Client;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Common;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public sealed record CampaignSpan(string From, string To, double Done, double Closing);

public sealed record NextDay(OutreachDay Day, int Routes);

public static class CampaignFacts
{
    private static readonly CultureInfo English = CultureInfo.InvariantCulture;
    private const char Together = '\u00a0';

    public static string StageName(CampaignStage stage) => stage switch
    {
        CampaignStage.Preparing => "Preparing",
        CampaignStage.WrappingUp => "Wrapping up",
        CampaignStage.Finished => "Finished",
        _ => "Ongoing",
    };

    public static string ForWalkers(CampaignStage stage) => stage switch
    {
        CampaignStage.Preparing => "Walkers don't see it yet. Mark out its area and plan its days.",
        CampaignStage.WrappingUp => "Walkers carry it, and the church has been told it ends soon.",
        CampaignStage.Finished => "Walkers no longer carry it. Its record of streets done is kept for looking back.",
        _ => "Walkers carry it now.",
    };

    public static string Countdown(Campaign campaign, CampaignStage stage, DateOnly today)
    {
        DateOnly? starts = OutreachRules.DateOf(campaign.StartsOn);
        DateOnly? ends = OutreachRules.DateOf(campaign.EndsOn);
        return stage switch
        {
            CampaignStage.Preparing => starts is { } start && start > today ? $"starts {In(start.DayNumber - today.DayNumber)}" : "no start date",
            CampaignStage.Ongoing => CampaignStages.WrapsUpOn(ends, CampaignStages.WrapUpDaysOf(campaign)) is { } wrap && wrap > today ? $"wraps up{Together}{Short(wrap).Replace(' ', Together)}" : "no end date",
            CampaignStage.WrappingUp => ends is { } end && end >= today ? $"ends {In(end.DayNumber - today.DayNumber)}" : "ends when moved on",
            _ => ends is { } ended ? $"ended {Short(ended)}" : "finished",
        };
    }

    public static string When(Campaign campaign, CampaignStage stage, DateOnly today) =>
        OutreachRules.DateOf(campaign.StartsOn) is null && OutreachRules.DateOf(campaign.EndsOn) is null
            ? Capital(Countdown(campaign, stage, today))
            : $"{OutreachWords.When(campaign)} · {Countdown(campaign, stage, today)}";

    public static CampaignSpan? Span(Campaign campaign, DateOnly today)
    {
        if (OutreachRules.DateOf(campaign.StartsOn) is not { } starts || OutreachRules.DateOf(campaign.EndsOn) is not { } ends)
            return null;
        double days = ends.DayNumber - starts.DayNumber + 1;
        double done = Math.Clamp((today.DayNumber - starts.DayNumber) / days, 0, 1);
        double closing = Math.Clamp(CampaignStages.WrapUpDaysOf(campaign) / days, 0, 1);
        return new CampaignSpan(Short(starts), Short(ends), done, closing);
    }

    public static string Handed(Campaign campaign, OutreachSettings settings)
    {
        string bundles = $"bundles{Together}of{Together}{CampaignStages.BundleSizeOf(campaign, settings)}";
        return campaign.HandedOut.Length > 0 ? $"{campaign.HandedOut} · {bundles}" : Capital(bundles);
    }

    public static string? BundlesOut(Campaign campaign, CampaignTotals totals, OutreachSettings settings)
    {
        int size = CampaignStages.BundleSizeOf(campaign, settings);
        int gone = CampaignStages.BundlesOut(totals.Letterboxes, size);
        if (totals.Letterboxes == 0)
            return campaign.BundlesMade > 0 ? $"{OutreachWords.Plural(campaign.BundlesMade, "bundle", "bundles")} made, none out yet" : null;
        return campaign.BundlesMade > 0 ? $"About {OutreachWords.Count(gone)} of {OutreachWords.Plural(campaign.BundlesMade, "bundle", "bundles")} out"
            : $"About {OutreachWords.Plural(gone, "bundle", "bundles")} out";
    }

    public static NextDay? Next(OutreachDesk desk, Campaign campaign, DateTime now) =>
        desk.Days.Where(day => day.Campaign == campaign.Id && day.EndsAt > now).OrderBy(day => day.StartsAt)
            .Select(day => new NextDay(day, desk.DayFacts.FirstOrDefault(facts => facts.DayId == day.Id)?.Routes ?? 0)).FirstOrDefault();

    public static string NextWords(NextDay? next, CampaignStage stage) =>
        next is { } day
            ? $"{ChurchTime.ToChurch(day.Day.StartsAt).ToString("ddd d MMM", English)} · {(day.Routes == 0 ? "no routes yet" : OutreachWords.Plural(day.Routes, "route", "routes"))}"
            : stage == CampaignStage.WrappingUp ? "No outreach day before it ends" : "No outreach day planned";

    public static string Told(Campaign campaign) =>
        OutreachRules.DateOf(campaign.ToldEndingOn) is { } told ? $"Told the church it ends soon, {Short(told)}" : "";

    public static int YearOf(Campaign campaign) =>
        (OutreachRules.DateOf(campaign.EndsOn) ?? OutreachRules.DateOf(campaign.StageSetOn) ?? DateOnly.MinValue).Year;

    public static string Short(DateOnly day) => day.ToString("d MMM", English);

    private static string In(int days) => days switch
    {
        0 => "today",
        1 => "tomorrow",
        < 21 => $"in{Together}{days}{Together}days",
        _ => $"in{Together}{days / 7}{Together}weeks",
    };

    private static string Capital(string words) => words.Length == 0 ? words : char.ToUpperInvariant(words[0]) + words[1..];
}
