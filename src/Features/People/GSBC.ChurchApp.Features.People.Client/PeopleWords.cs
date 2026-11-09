using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Client;

public static class PeopleWords
{
    public const string WhoSees = "Only you, your parents and the church's safeguarding team see it.";

    public static string Age(DateTime? born, DateOnly today) =>
        born is { } day ? Ages.YearsOn(WireDates.FromWire(day), today).ToString() : "Age not given";

    public static string SignIn(bool canSignIn) => canSignIn ? "signs in on their own phone" : "no sign-in of their own";

    public static string ChildLine(PersonCard child, DateOnly today) => $"{Age(child.BirthDate, today)} · {SignIn(child.CanSignIn)}";

    public static string Day(DateTime? day) => day is { } value ? $"{value:d MMM yyyy}" : "";

    public static string AskTitle(LinkAsk ask) =>
        ask.AskedByChild ? $"{ask.ChildName} asks you to be their parent" : $"{ask.AskedByName} asks you to look after {ask.ChildName} too";

    public static DateOnly? ParseDay(string text) => DateOnly.TryParseExact(text, "yyyy-MM-dd", out DateOnly day) ? day : null;

    public static string FieldDay(DateTime? day) => day is { } value ? $"{value:yyyy-MM-dd}" : "";
}
