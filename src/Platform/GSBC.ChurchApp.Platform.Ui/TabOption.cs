namespace GSBC.ChurchApp.Platform.Ui;

public sealed record TabOption<TValue>(TValue Value, string Label, int? Count = null);
