namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public static class OneNotification
{
    public static T? Choose<T>(IEnumerable<T> mostSpecificFirst, Func<T, bool> reaches, Func<T, bool> wanted) where T : class =>
        mostSpecificFirst.FirstOrDefault(each => reaches(each) && wanted(each));

    public static IEnumerable<InboxItem> OnePerThing(IEnumerable<InboxItem> reached, Func<string, bool> wanted) =>
        reached.GroupBy(item => item.OneOf ?? item.Id)
            .Select(related => Choose(related.OrderBy(item => item.Rank), _ => true, item => wanted(item.Kind)))
            .OfType<InboxItem>();
}
