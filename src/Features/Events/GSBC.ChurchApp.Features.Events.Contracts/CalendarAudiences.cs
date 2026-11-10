using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Contracts;

public static class CalendarAudiences
{
    public const string EveryoneWords = "everyone with the app";
    public const string SignedInWords = "anyone signed in";

    public static Audience Of(string text) =>
        Audience.TryParse(text, out Audience? audience) && (audience.Node is null || audience.At is not null) ? audience : Audience.SignedIn;

    public static Audience Union(IEnumerable<Audience> audiences)
    {
        List<Audience> all = audiences.ToList();
        if (all.Contains(Audience.Everyone))
            return Audience.Everyone;
        if (all.Count == 0 || all.Any(audience => !audience.IsForSomeone))
            return Audience.SignedIn;
        if (all.FirstOrDefault(audience => audience.At is not null) is { } oneGroup)
            return oneGroup;
        return Audience.HoldingAnyRole(all.SelectMany(audience => audience.RoleIds));
    }

    public static Audience SignedInPart(Audience audience) => audience == Audience.Everyone ? Audience.SignedIn : audience;

    public static Widening Widening(Audience current, Audience added)
    {
        Audience union = Union([current, added]);
        if (union == current)
            return new Widening(false, "", []);
        if (union == Audience.Everyone)
            return new Widening(true, EveryoneWords, []);
        if (union == Audience.SignedIn)
            return new Widening(true, SignedInWords, []);
        return new Widening(true, "", union.RoleIds.Except(current.RoleIds).ToList());
    }
}

public sealed record Widening(bool Widens, string Words, IReadOnlyList<Guid> NewRoleIds);
