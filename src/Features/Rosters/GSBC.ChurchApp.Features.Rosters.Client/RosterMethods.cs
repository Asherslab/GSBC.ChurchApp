namespace GSBC.ChurchApp.Features.Rosters.Client;

public static class RosterMethods
{
    public const string Under = "/gsbc.rosters.";
    private const string Rosters = "/gsbc.rosters.Rosters/";

    public const string SavePosition = Rosters + "SavePosition";
    public const string DeletePosition = Rosters + "DeletePosition";
    public const string SetTrained = Rosters + "SetTrained";
    public const string SaveSettings = Rosters + "SaveSettings";
    public const string AddDate = Rosters + "AddDate";
    public const string RemoveDate = Rosters + "RemoveDate";
    public const string Assign = Rosters + "Assign";
    public const string Unassign = Rosters + "Unassign";
    public const string DecideSwap = Rosters + "DecideSwap";
    public const string Reply = Rosters + "Reply";
    public const string AskSwap = Rosters + "AskSwap";
    public const string TakeSwap = Rosters + "TakeSwap";
    public const string WithdrawSwap = Rosters + "WithdrawSwap";
    public const string SaveAway = Rosters + "SaveAway";
    public const string RemoveAway = Rosters + "RemoveAway";

    public static string Describe(string method) => method[(method.LastIndexOf('/') + 1)..] switch
    {
        "SavePosition" or "DeletePosition" or "SetTrained" => "a change to the positions",
        "SaveSettings" => "the roster's settings",
        "AddDate" or "RemoveDate" => "a change to the dates",
        "Assign" or "Unassign" => "a change to who's on",
        "DecideSwap" => "your answer to a swap",
        "Reply" => "your answer",
        "AskSwap" or "WithdrawSwap" => "your swap",
        "TakeSwap" => "taking a place",
        "SaveAway" or "RemoveAway" => "the days you're away",
        _ => "a change",
    };
}
