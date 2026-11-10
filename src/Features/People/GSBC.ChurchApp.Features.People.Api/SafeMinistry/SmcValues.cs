using GSBC.ChurchApp.Features.People.Api.Data;
using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Api.SafeMinistry;

internal static class SmcReasons
{
    public const string PoliceCheck = "their police check isn't cleared";
    public const string Referee = "a referee's answer needs looking at";
    public const string CardNotVerified = "their blue card couldn't be verified there";
    public const string Disclosure = "something in their paperwork needs looking at";
    public const string NoCard = "they work with children but have no blue card there";
    public const string Gone = "they're no longer in Safe Ministry Check";

    public static string Status(string status) => $"it says “{status}”";
}

internal sealed record SmcValues(
    string? CardNumber,
    string? CardStatus,
    DateOnly? CardExpires,
    DateOnly? CardChecked,
    string? TrainingProgress,
    DateOnly? TrainingDue,
    string? OtherTrainingName,
    DateOnly? OtherTrainingDone,
    string? Status,
    string? StatusWords,
    List<string> CheckReasons)
{
    private const int BlueCardNumberLength = 30;
    private const int LongestText = 300;

    private static readonly string[] AllGood = ["approved", "complete", "completed", "current", "active", "cleared", "valid", "verified"];
    private static readonly string[] NothingToSay = ["none", "no", "n/a", "na", "nil", "0"];
    private static readonly string[] NoPoliceCheckNeeded = ["n/a", "na", "not required", "none"];

    public static SmcValues From(SmcRow row)
    {
        string cardNumber = row["wwcc_id"];
        string cardStatus = CardStatusOf(row["wwcc_status"]);
        string status = row["status"];
        return new SmcValues(
            Text(cardNumber, BlueCardNumberLength),
            Text(cardStatus),
            row.Day("wwcc_expiry"),
            row.Day("wwcc_checked"),
            Text(row["course_progress"]),
            row.Day("next_action_date"),
            Text(row["other_training_name"]),
            row.Day("other_training_completed_date"),
            Text(status),
            Text(row["status_description"]),
            Reasons(row, cardNumber, cardStatus, status));
    }

    public void ApplyTo(SafeguardingEntry entry)
    {
        entry.CardNumber = CardNumber;
        entry.CardStatus = CardStatus;
        entry.CardExpires = CardExpires;
        entry.CardChecked = CardChecked;
        entry.TrainingDone = null;
        entry.TrainingProgress = TrainingProgress;
        entry.TrainingDue = TrainingDue;
        entry.OtherTrainingName = OtherTrainingName;
        entry.OtherTrainingDone = OtherTrainingDone;
        entry.SmcStatus = Status;
        entry.SmcStatusWords = StatusWords;
        if (!entry.CheckReasons.SequenceEqual(CheckReasons))
            entry.CheckReasons = CheckReasons;
    }

    private static List<string> Reasons(SmcRow row, string cardNumber, string cardStatus, string status)
    {
        List<string> reasons = [];
        if (status.Length > 0 && !Good(status))
            reasons.Add(SmcReasons.Status(Text(status, 60)!));
        if (row["wwcc_status"].Length > 0 && cardStatus.Length == 0)
            reasons.Add(SmcReasons.CardNotVerified);
        if (Said(row["police_check_status"]) && !Good(row["police_check_status"]) && !NoPoliceCheckNeeded.Contains(row["police_check_status"].ToLowerInvariant()))
            reasons.Add(SmcReasons.PoliceCheck);
        if (new[] { row["referee1_response"], row["referee2_response"] }.Any(answer => Said(answer) && !answer.StartsWith("positive", StringComparison.OrdinalIgnoreCase)))
            reasons.Add(SmcReasons.Referee);
        if (Said(row["trainee_disclosure"]) && !Good(status))
            reasons.Add(SmcReasons.Disclosure);
        if (row["works_with_children"] == "1" && cardNumber.Length == 0)
            reasons.Add(SmcReasons.NoCard);
        return reasons;
    }

    private static string CardStatusOf(string words) => words.Trim().ToLowerInvariant() switch
    {
        "current" or "valid" or "active" or "approved" or "cleared" or "issued" => BlueCards.Current,
        "exemption" or "exempt" => BlueCards.Exemption,
        "applied" or "applied for" or "application lodged" or "pending" => BlueCards.Applied,
        "suspended" => BlueCards.Suspended,
        "cancelled" or "canceled" or "revoked" => BlueCards.Cancelled,
        "expired" => BlueCards.Expired,
        _ => "",
    };

    private static bool Good(string words) => AllGood.Contains(words.Trim().ToLowerInvariant());

    private static bool Said(string words) => words.Trim().Length > 0 && !NothingToSay.Contains(words.Trim().ToLowerInvariant());

    private static string? Text(string text, int longest = LongestText) => text.Trim() is { Length: > 0 } trimmed ? trimmed[..Math.Min(trimmed.Length, longest)] : null;
}
