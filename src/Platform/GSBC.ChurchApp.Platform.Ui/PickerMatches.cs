namespace GSBC.ChurchApp.Platform.Ui;

internal static class PickerMatches
{
    public static List<TItem> Of<TItem>(IEnumerable<TItem> items, Func<TItem, string> text, Func<TItem, string>? detail, string query, int most) => items
        .Where(item => query.Length == 0 || text(item).Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || (detail?.Invoke(item).Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false))
        .OrderBy(item => query.Length == 0 || text(item).StartsWith(query, StringComparison.CurrentCultureIgnoreCase) ? 0
            : text(item).Contains(query, StringComparison.CurrentCultureIgnoreCase) ? 1 : 2)
        .Take(most)
        .ToList();
}
