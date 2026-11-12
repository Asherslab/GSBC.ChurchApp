namespace GSBC.ChurchApp.Platform.Ui;

public sealed record BoardColumn<TColumn>(
    TColumn Value,
    string Title,
    string Empty = "Nothing here.",
    bool Narrow = false,
    bool Folds = false,
    int? Shown = null,
    Func<int, string>? More = null,
    bool Addable = false);
