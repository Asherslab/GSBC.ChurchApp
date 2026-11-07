using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Events.Api;

internal static class CalendarAudiences
{
    public static Audience Of(string text) =>
        Audience.TryParse(text, out Audience? audience) && audience.Node is null ? audience : Audience.SignedIn;

    public static Audience Union(IEnumerable<Audience> audiences)
    {
        List<Audience> all = audiences.ToList();
        if (all.Contains(Audience.Everyone))
            return Audience.Everyone;
        if (all.Count == 0 || all.Any(audience => !audience.IsForRoles))
            return Audience.SignedIn;
        return Audience.HoldingAnyRole(all.SelectMany(audience => audience.RoleIds));
    }

    public static Audience SignedInPart(Audience audience) => audience == Audience.Everyone ? Audience.SignedIn : audience;
}
