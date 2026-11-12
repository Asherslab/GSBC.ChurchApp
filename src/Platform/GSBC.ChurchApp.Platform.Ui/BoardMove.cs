namespace GSBC.ChurchApp.Platform.Ui;

public sealed record BoardMove<TItem, TColumn>(TItem Item, TColumn To);
