namespace Gsbc.AppPoc.Components;

public sealed record ToolRailItem(string Key, string Icon, string Label, string? Shortcut = null, bool? Pressed = null, bool Disabled = false);
