namespace GSBC.ChurchApp.Features.Rosters.Contracts;

public static class RosterParts
{
    public const string Key = "rosters";
}

public static class Replies
{
    public const string Asked = "asked";
    public const string Accepted = "accepted";
    public const string Declined = "declined";
    public const string On = "on";

    public static bool Fills(string state) => state != Declined;

    public static bool Waiting(string state) => state == Asked;
}

public static class SwapWays
{
    public const string Open = "open";
    public const string Team = "team";
    public const string Approve = "approve";
    public const string Direct = "direct";
}

public static class OpenSwaps
{
    public const string Off = "off";
    public const string Team = SwapWays.Team;
    public const string Approve = SwapWays.Approve;

    public static readonly IReadOnlyList<string> All = [Off, Team, Approve];

    public static bool Valid(string value) => All.Contains(value);
}

public static class SwapStates
{
    public const string Open = "open";
    public const string Offered = "offered";
    public const string Done = "done";
    public const string Declined = "declined";

    public static bool Live(string state) => state is Open or Offered;
}

public static class WhoSees
{
    public const string Everything = "all";
    public const string WithYou = "with-you";
    public const string Yours = "yours";

    public static bool Valid(string value) => value is Everything or WithYou or Yours;
}

public static class RosterDefaults
{
    public const int RemindDaysBefore = 2;
    public const int NudgeAfterDays = 3;
    public const string FormerMember = "Someone who has left";
}
