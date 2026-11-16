using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Ui;

internal static class PopoverPlacement
{
    public static async Task FloatAsync(IJSRuntime js, ElementReference popover)
    {
        try
        {
            await using IJSObjectReference module = await js.InvokeAsync<IJSObjectReference>("import", "./_content/GSBC.ChurchApp.Platform.Ui/js/menu.js");
            await module.InvokeVoidAsync("float", popover);
        }
        catch (JSException)
        {
        }
    }
}
