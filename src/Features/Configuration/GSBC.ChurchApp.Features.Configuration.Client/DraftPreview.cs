using System.Globalization;
using GSBC.ChurchApp.Platform.Client.Configuration;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Features.Configuration.Client;

internal sealed class DraftPreview(IJSRuntime js) : IPreviewMode
{
    private const string DraftKey = "gsbc.preview.draft";
    private const string NameKey = "gsbc.preview.name";
    private const string ViewerKey = "gsbc.preview.as";
    private const string AtKey = "gsbc.preview.at";

    private bool _loaded;

    public PreviewChoice? Current { get; private set; }

    public event Action? Changed;

    public async Task LoadAsync()
    {
        if (_loaded)
            return;
        _loaded = true;

        string? draft = await ReadAsync(DraftKey);
        string? name = await ReadAsync(NameKey);
        PreviewViewer viewer = Enum.TryParse(await ReadAsync(ViewerKey), out PreviewViewer parsed) ? parsed : PreviewViewer.Me;
        DateTime? at = DateTime.TryParse(await ReadAsync(AtKey), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime moment)
            ? moment
            : null;
        PreviewChoice choice = new(draft, name ?? "", viewer, at);
        Current = choice.ChangesAnything ? choice : null;
        Changed?.Invoke();
    }

    public async Task ChooseAsync(PreviewChoice? choice)
    {
        Current = choice is { ChangesAnything: true } ? choice : null;
        await WriteAsync(DraftKey, Current?.DraftId);
        await WriteAsync(NameKey, Current?.Name);
        await WriteAsync(ViewerKey, Current is null ? null : Current.As.ToString());
        await WriteAsync(AtKey, Current?.At?.ToString("O", CultureInfo.InvariantCulture));
        Changed?.Invoke();
    }

    private async Task<string?> ReadAsync(string key)
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", key);
        }
        catch (JSException)
        {
            return null;
        }
    }

    private async Task WriteAsync(string key, string? value)
    {
        try
        {
            if (value is null)
                await js.InvokeVoidAsync("localStorage.removeItem", key);
            else
                await js.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (JSException)
        {
        }
    }
}
