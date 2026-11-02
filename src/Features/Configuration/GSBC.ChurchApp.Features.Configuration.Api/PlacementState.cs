using System.Text.Json;
using GSBC.ChurchApp.Features.Configuration.Api.Data;
using GSBC.ChurchApp.Features.Configuration.Contracts;
using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Features.Configuration.Api;

internal sealed record PlacementState(
    string Surface, int Order, bool Hidden, string Audience, DateTime? StartsAt, DateTime? EndsAt, SortedDictionary<string, string> Values)
{
    public string ToJson() => JsonSerializer.Serialize(this, JsonSerializerOptions.Default);

    public static PlacementState? FromJson(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<PlacementState>(json, JsonSerializerOptions.Default);

    public static PlacementState Of(PlacementRecord record) =>
        new(record.Surface, record.Order, record.Hidden, record.Audience, record.StartsAt, record.EndsAt, new(record.Values));

    public static PlacementState Of(Placement placement) =>
        new(placement.Surface, placement.Order, placement.Hidden, placement.Audience.Trim(), AsUtc(placement.StartsAt), AsUtc(placement.EndsAt),
            new(placement.Values.GroupBy(value => value.Key).ToDictionary(group => group.Key, group => group.Last().Value)));

    public static PlacementState Of(StoredPlacement placement) =>
        new(placement.Surface, placement.Order, placement.Hidden, placement.Audience.Trim(), AsUtc(placement.StartsAt), AsUtc(placement.EndsAt),
            new(placement.Values.ToDictionary()));

    public StoredPlacement ToStored(Guid id, long revision) =>
        new(id.ToString(), Surface, Order, Hidden, Audience, StartsAt, EndsAt, Values, revision);

    public void ApplyTo(PlacementRecord record)
    {
        record.Order = Order;
        record.Hidden = Hidden;
        record.Audience = Audience;
        record.StartsAt = StartsAt;
        record.EndsAt = EndsAt;
        record.Values = new Dictionary<string, string>(Values);
    }

    public Placement ToMessage(Guid id, string area, string? scopeId) => new()
    {
        Id = id.ToString(),
        Area = area,
        ScopeId = scopeId,
        Surface = Surface,
        Order = Order,
        Hidden = Hidden,
        Audience = Audience,
        StartsAt = StartsAt,
        EndsAt = EndsAt,
        Values = Values.Select(pair => new SettingValue { Key = pair.Key, Value = pair.Value }).ToList(),
    };

    private static DateTime? AsUtc(DateTime? value) => value is { } utc ? DateTime.SpecifyKind(utc, DateTimeKind.Utc) : null;
}
