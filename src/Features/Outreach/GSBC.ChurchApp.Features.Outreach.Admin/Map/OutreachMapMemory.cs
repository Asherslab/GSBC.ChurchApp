using System.Text.Json;
using System.Text.Json.Serialization;
using GSBC.ChurchApp.Platform.Ui.Maps;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Map;

public sealed record OutreachMapRemembered(OutreachMapJob Job, bool Open, IReadOnlyList<OutreachLayer> Layers, MapFrame? View, string? Campaign)
{
    public static readonly OutreachMapRemembered FirstTime = new(OutreachMapJob.Coverage, true,
        [OutreachLayer.Coverage, OutreachLayer.SkippedPlaces, OutreachLayer.Marks, OutreachLayer.Reports], null, null);
}

public sealed class OutreachMapMemory(IJSRuntime js)
{
    private const string Key = "gsbc:outreach-map";

    private OutreachMapRemembered? _remembered;

    public async Task<OutreachMapRemembered> LastAsync()
    {
        if (_remembered is not null)
            return _remembered;
        try
        {
            _remembered = await js.InvokeAsync<string?>("localStorage.getItem", Key) is { Length: > 0 } json
                ? JsonSerializer.Deserialize(json, OutreachMapJson.Default.OutreachMapRemembered) ?? OutreachMapRemembered.FirstTime
                : OutreachMapRemembered.FirstTime;
        }
        catch (Exception exception) when (exception is JSException or JsonException or NotSupportedException)
        {
            _remembered = OutreachMapRemembered.FirstTime;
        }
        return _remembered;
    }

    public async Task RememberAsync(OutreachMapRemembered remembered)
    {
        _remembered = remembered;
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(remembered, OutreachMapJson.Default.OutreachMapRemembered));
        }
        catch (JSException)
        {
        }
    }
}

[JsonSerializable(typeof(OutreachMapRemembered))]
internal sealed partial class OutreachMapJson : JsonSerializerContext;
