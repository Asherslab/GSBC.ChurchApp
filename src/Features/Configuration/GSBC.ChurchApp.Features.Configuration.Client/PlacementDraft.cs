using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Client;

internal sealed class PlacementDraft
{
    private PlacementDraft(Surface surface) => Surface = surface;

    public Surface Surface { get; }
    public string? Id { get; private init; }
    public long Revision { get; private init; }
    public int Order { get; set; }
    public bool Hidden { get; set; }
    public string Audience { get; set; } = Platform.Contracts.Configuration.Audience.Everyone.Text;
    public DateOnly? ShowFrom { get; set; }
    public DateOnly? ShowUntil { get; set; }
    public Dictionary<string, string> Values { get; private init; } = [];

    public bool IsNew => Id is null;

    public static PlacementDraft Of(ArrangedSurface arranged) => new(arranged.Surface)
    {
        Id = arranged.Placement?.Id,
        Revision = arranged.Placement?.Revision ?? 0,
        Order = arranged.Order,
        Hidden = arranged.Hidden,
        Audience = arranged.Audience.Text,
        ShowFrom = arranged.StartsAt is { } starts ? DateOnly.FromDateTime(ChurchTime.ToChurch(starts)) : null,
        ShowUntil = arranged.EndsAt is { } ends ? DateOnly.FromDateTime(ChurchTime.ToChurch(ends).AddTicks(-1)) : null,
        Values = new Dictionary<string, string>(arranged.Values.Raw),
    };

    public static PlacementDraft New(Surface surface, int order) => new(surface)
    {
        Order = order,
        Values = new Dictionary<string, string>(surface.Defaults.Raw),
    };

    public IReadOnlyList<string> Problems
    {
        get
        {
            List<string> problems = [.. Surface.ProblemsWith(Values)];
            if (ShowFrom is { } from && ShowUntil is { } until && until < from)
                problems.Add("The last day can't be before the first.");
            return problems;
        }
    }

    public Placement ToMessage() => new()
    {
        Id = Id ?? "",
        Revision = Revision,
        Surface = Surface.Key,
        Order = Order,
        Hidden = Hidden,
        Audience = Audience,
        StartsAt = ShowFrom is { } from ? ChurchTime.ToUtc(from.ToDateTime(TimeOnly.MinValue)) : null,
        EndsAt = ShowUntil is { } until ? ChurchTime.ToUtc(until.AddDays(1).ToDateTime(TimeOnly.MinValue)) : null,
        Values = Surface.Settings
            .Where(setting => Values.TryGetValue(setting.Key, out string? value) && value != setting.DefaultText)
            .Select(setting => new SettingValue { Key = setting.Key, Value = Values[setting.Key] })
            .ToList(),
    };
}
