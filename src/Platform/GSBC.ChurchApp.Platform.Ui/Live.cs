namespace GSBC.ChurchApp.Platform.Ui;

public sealed record Live<TValue>(TValue Value, bool Stale);
