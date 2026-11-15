namespace GSBC.ChurchApp.Platform.Ui.Maps;

public sealed record MapWorkspaceJob(string Id, string Label, string Icon, int Count = 0, bool StartsGroup = false);
