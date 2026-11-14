namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public static class OutreachMethods
{
    public const string Under = "/gsbc.outreach.Outreach/";

    public static string Describe(string method) => method[Under.Length..] switch
    {
        "RecordWalk" => "Part of a walk",
        "NoJunkMail" => "A No junk mail mark",
        "UndoNoJunkMail" => "Taking back a No junk mail mark",
        "Door" => "What happened at a door",
        "Report" => "A report about a place",
        "Claim" => "Taking a route",
        "Unclaim" => "Giving back a route",
        "Counting" => "Being counted by name",
        _ => "An outreach change",
    };
}
