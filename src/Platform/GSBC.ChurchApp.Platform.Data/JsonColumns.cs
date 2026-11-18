using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GSBC.ChurchApp.Platform.Data;

public static class JsonColumns
{
    public static PropertyBuilder<T?> AsJson<T>(this PropertyBuilder<T?> property) where T : class =>
        property.HasColumnType("jsonb").HasConversion(
            value => value == null ? null : JsonSerializer.Serialize(value, JsonSerializerOptions.Web),
            json => json == null ? null : JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web),
            new ValueComparer<T?>(
                (first, second) => JsonSerializer.Serialize(first, JsonSerializerOptions.Web) == JsonSerializer.Serialize(second, JsonSerializerOptions.Web),
                value => JsonSerializer.Serialize(value, JsonSerializerOptions.Web).GetHashCode(StringComparison.Ordinal),
                value => value == null ? null : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, JsonSerializerOptions.Web), JsonSerializerOptions.Web)));
}
