using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Ui;

public sealed record MenuItem(string Label, EventCallback OnChoose, string? Icon = null, string? Detail = null, bool Checked = false, bool Danger = false, bool StartsGroup = false, bool Disabled = false);

public static class MenuItems
{
    public static MenuItem Item(this IHandleEvent owner, string label, Func<Task> choose, string? icon = null, string? detail = null,
        bool @checked = false, bool danger = false, bool startsGroup = false, bool disabled = false) =>
        new(label, EventCallback.Factory.Create(owner, choose), icon, detail, @checked, danger, startsGroup, disabled);

    public static MenuItem Item(this IHandleEvent owner, string label, Action choose, string? icon = null, string? detail = null,
        bool @checked = false, bool danger = false, bool startsGroup = false, bool disabled = false) =>
        new(label, EventCallback.Factory.Create(owner, choose), icon, detail, @checked, danger, startsGroup, disabled);
}
