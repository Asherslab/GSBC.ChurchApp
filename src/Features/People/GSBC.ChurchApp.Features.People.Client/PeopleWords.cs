using GSBC.ChurchApp.Features.People.Contracts;

namespace GSBC.ChurchApp.Features.People.Client;

public static class PeopleWords
{
    public const string WhoSees = "Only you, your parents and the church's safeguarding team see it.";

    public static string Age(DateTime? born, DateOnly today) =>
        born is { } day ? Ages.YearsOn(WireDates.FromWire(day), today).ToString() : "Age not given";

    public static string SignIn(bool canSignIn) => canSignIn ? "signs in on their own phone" : "no sign-in of their own";

    public static string ChildLine(PersonCard child, DateOnly today) => $"{Age(child.BirthDate, today)} · {SignIn(child.CanSignIn)}";

    public static string EffectiveReach(PersonCard child, DateOnly today) =>
        Reach.Effective(child.Reach, Ages.IsMinor(WireDates.FromWire(child.BirthDate), today), child.CanSignIn, hasParents: true);

    public static string ReachForChild(PersonFamily me, DateOnly today) =>
        Reach.Effective(me.Reach, Ages.IsMinor(WireDates.FromWire(me.BirthDate), today), me.CanSignIn, hasParents: me.Parents.Count > 0) switch
        {
            Reach.ThroughParents => "Your ministries reach you through your parents: roster requests and messages go to them, labelled with your name, and they can answer for you.",
            Reach.Both => "Your ministries reach you and your parents: roster requests and messages come to you, and your parents see them and can answer for you too.",
            _ => "Your ministries reach you directly. Your parents still see your details.",
        };

    public static string ReachForParent(PersonCard child, DateOnly today) => EffectiveReach(child, today) switch
    {
        Reach.ThroughParents => "reached through you",
        Reach.Both => "reached directly and through you",
        _ => "reached directly",
    };

    public static string Day(DateTime? day) => day is { } value ? $"{value:d MMM yyyy}" : "";

    public static string When(DateTime at) => $"{at.ToLocalTime():d MMM yyyy}";

    public static string AskTitle(LinkAsk ask) =>
        ask.AskedByChild ? $"{ask.ChildName} asks you to be their parent" : $"{ask.AskedByName} asks you to look after {ask.ChildName} too";

    public static string Problem(Grpc.Core.RpcException exception) => exception.StatusCode switch
    {
        Grpc.Core.StatusCode.InvalidArgument or Grpc.Core.StatusCode.FailedPrecondition or Grpc.Core.StatusCode.NotFound
            or Grpc.Core.StatusCode.PermissionDenied => exception.Status.Detail,
        _ => "That didn't go through. Check you're online and try again.",
    };

    public static DateOnly? ParseDay(string text) => DateOnly.TryParseExact(text, "yyyy-MM-dd", out DateOnly day) ? day : null;

    public static string FieldDay(DateTime? day) => day is { } value ? $"{value:yyyy-MM-dd}" : "";
}
