namespace GSBC.ChurchApp.Features.Ministries.Contracts;

public static class ChurchNewsStates
{
    public const string None = "";
    public const string Asked = "asked";
    public const string Shared = "shared";
    public const string Declined = "declined";

    public static string Line(string state) => state switch
    {
        Asked => "Asked for church news",
        Shared => "In church news",
        Declined => "Kept in this ministry's news",
        _ => "",
    };
}
