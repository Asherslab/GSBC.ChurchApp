using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;
using GSBC.ChurchApp.Platform.Permissions;
using GSBC.ChurchApp.Platform.Ui.Surfaces;

namespace GSBC.ChurchApp.Features.Configuration.Client;

public sealed record StylingPreview(string Who, ThemeScheme Scheme, DateOnly? Day, PhoneDevice Device)
{
    public const string Anyone = "every";
    public const string Visitor = "visitor";
    public const string SignedIn = "signed-in";

    public static StylingPreview Start => new(Visitor, ThemeScheme.Light, null, PhoneDevice.IPhone15Pro);

    public DateTime At(DateTime now) => Day is { } day ? ChurchTime.ToUtc(day.ToDateTime(new TimeOnly(9, 0))) : now;

    public bool Shows(ArrangedSurface entry, DateTime now) => Who == Anyone
        ? !entry.Hidden && InDates(entry, At(now))
        : entry.IsShownTo(Viewer, At(now));

    private Viewer Viewer => Who switch
    {
        SignedIn => new Viewer(true, _ => false),
        _ when Guid.TryParse(Who, out Guid role) => new Viewer(true, node => node == RoleMembership.NodeOf(role)),
        _ => Viewer.Anonymous,
    };

    public string Words(IReadOnlyDictionary<Guid, string> roleNames) => Who switch
    {
        Anyone => "anyone, with every tile",
        SignedIn => "someone signed in",
        _ when Guid.TryParse(Who, out Guid role) => $"someone who's {roleNames.GetValueOrDefault(role, "in that role")}",
        _ => "a visitor",
    };

    private static bool InDates(ArrangedSurface entry, DateTime at) =>
        (entry.StartsAt is null || at >= entry.StartsAt) && (entry.EndsAt is null || at < entry.EndsAt);
}
