using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Ui.Content;

public sealed record AppCardAction(string Label, string? Icon = null, string? Href = null, EventCallback OnClick = default, bool Main = false, bool Busy = false);
