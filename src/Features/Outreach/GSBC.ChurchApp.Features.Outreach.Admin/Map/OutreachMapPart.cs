using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Features.Outreach.Admin.Map;

public abstract class OutreachMapPart : ComponentBase, IDisposable
{
    [CascadingParameter] public required OutreachMapContext Map { get; set; }

    protected override void OnInitialized() => Map.Changed += Rerender;

    public virtual void Dispose()
    {
        Map.Changed -= Rerender;
        GC.SuppressFinalize(this);
    }

    private void Rerender() => _ = InvokeAsync(StateHasChanged);
}
