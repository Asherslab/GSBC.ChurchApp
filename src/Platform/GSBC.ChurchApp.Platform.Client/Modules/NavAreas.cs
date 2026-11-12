namespace GSBC.ChurchApp.Platform.Client.Modules;

public static class NavAreas
{
    public static readonly NavArea SundaysAndSermons = new("Sundays & sermons", 1);
    public static readonly NavArea ThisWeek = new("This week", 2);
    public static readonly NavArea MinistriesAndGroups = new("Ministries & groups", 3);
    public static readonly NavArea Safety = new("Safety", 4);
    public static readonly NavArea PeopleAndAccess = new("People & access", 5);
    public static readonly NavArea TheApp = new("The app", 6);
    public static readonly NavArea Technical = new("Technical", 7);
    public static readonly NavArea You = new("You", 9, AtFoot: true);
}
