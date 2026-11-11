namespace GSBC.ChurchApp.Features.Ministries.Client;

public static class MinistryMethods
{
    public const string Under = "/gsbc.ministries.";
    private const string Ministries = "/gsbc.ministries.Ministries/";
    private const string Leading = "/gsbc.ministries.Leading/";

    public const string Join = Ministries + "Join";
    public const string Leave = Ministries + "Leave";
    public const string Withdraw = Ministries + "Withdraw";
    public const string ParentAnswer = Ministries + "ParentAnswer";
    public const string Answer = Leading + "Answer";
    public const string ChangePerson = Leading + "ChangePerson";
    public const string SetRole = Leading + "SetRole";
    public const string LinkParent = Leading + "LinkParent";
    public const string SavePost = Leading + "SavePost";
    public const string DeletePost = Leading + "DeletePost";
    public const string SaveFile = Leading + "SaveFile";
    public const string DeleteFile = Leading + "DeleteFile";
    public const string SaveMinistry = Leading + "SaveMinistry";
    public const string SaveParts = Leading + "SaveParts";
    public const string Move = Leading + "Move";
    public const string AddSubMinistry = Leading + "AddSubMinistry";
    public const string Create = Leading + "Create";
    public const string Delete = Leading + "Delete";
    public const string Archive = Leading + "Archive";
    public const string SaveCalendar = Leading + "SaveCalendar";
    public const string RemoveCalendar = Leading + "RemoveCalendar";
    public const string SaveSession = Leading + "SaveSession";

    public static string Describe(string method) => method[(method.LastIndexOf('/') + 1)..] switch
    {
        "Join" => "your ask to join",
        "Leave" => "leaving",
        "Withdraw" => "taking back your ask",
        "ParentAnswer" => "your answer for your child",
        "Answer" => "your answer to an ask",
        "ChangePerson" or "SetRole" or "LinkParent" => "a change to its people",
        "SavePost" => "a post",
        "DeletePost" => "deleting a post",
        "SaveFile" => "a file",
        "DeleteFile" => "deleting a file",
        "SaveMinistry" or "SaveParts" => "its settings",
        "Move" => "moving it",
        "AddSubMinistry" or "Create" => "a new ministry",
        "Delete" => "deleting it",
        "Archive" => "archiving it",
        "SaveCalendar" or "RemoveCalendar" => "a calendar change",
        "SaveSession" => "a Sunday change",
        _ => "a change",
    };
}
