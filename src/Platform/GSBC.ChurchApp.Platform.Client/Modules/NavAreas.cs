namespace GSBC.ChurchApp.Platform.Client.Modules;

public static class NavAreas
{
    public static readonly NavArea SundaysAndSermons = new("Sundays & sermons", 1);
    public static readonly NavArea CalendarAndNews = new("Calendar & news", 2);
    public static readonly NavArea TheApp = new("The app", 3);
    public static readonly NavArea People = new("People", 4);
    public static readonly NavArea You = new("You", 9, AtFoot: true);
}
