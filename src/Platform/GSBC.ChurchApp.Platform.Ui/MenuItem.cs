namespace GSBC.ChurchApp.Platform.Ui;

public sealed record MenuItem(string Label, Func<Task> OnChoose, string? Icon = null, string? Detail = null, bool Checked = false, bool Danger = false, bool StartsGroup = false, bool Disabled = false);
