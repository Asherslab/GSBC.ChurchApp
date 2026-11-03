namespace GSBC.ChurchApp.Platform.Ui;

public sealed record FieldOption(string Value, string Label);

public sealed record SegmentOption<TValue>(TValue Value, string Label, string? Icon = null);

public sealed record FilterOption<TValue>(TValue Value, string Label, int? Count = null);
