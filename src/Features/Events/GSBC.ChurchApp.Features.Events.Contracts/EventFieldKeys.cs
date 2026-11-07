namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class EventFieldKeys
{
    public const string Title = "title";
    public const string Label = "label";
    public const string Time = "time";
    public const string Picture = "picture";
    public const string Description = "description";
    public const string Place = "place";
    public const string Contacts = "contacts";
    public const string Register = "register";
    public const string Links = "links";

    public static readonly IReadOnlyList<string> All = [Title, Label, Time, Picture, Description, Place, Contacts, Register, Links];

    public static readonly IReadOnlyList<string> Needable = [Place, Contacts, Description, Picture, Register];

    public static string Missing(string key) => key switch
    {
        Place => "Place to be announced",
        Contacts => "Contact to be announced",
        Description => "Details to come",
        Picture => "Picture to come",
        Register => "Registration to open",
        _ => "Details to come",
    };
}
