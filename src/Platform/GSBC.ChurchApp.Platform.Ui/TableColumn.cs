using Microsoft.AspNetCore.Components;

namespace GSBC.ChurchApp.Platform.Ui;

public sealed record TableColumn<TItem>(string Title, RenderFragment<TItem> Cell, Func<TItem, IComparable?>? SortBy = null, string? Class = null);
