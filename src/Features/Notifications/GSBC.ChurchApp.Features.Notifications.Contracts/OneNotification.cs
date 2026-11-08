namespace GSBC.ChurchApp.Features.Notifications.Contracts;

public sealed record ChosenNotification<T>(T Chosen, bool Push);

public static class OneNotification
{
    public static ChosenNotification<T>? Choose<T>(IEnumerable<T> mostSpecificFirst, Func<T, bool> reaches, Func<T, bool> pushes)
    {
        List<T> reached = mostSpecificFirst.Where(reaches).ToList();
        if (reached.Count == 0)
            return null;
        return reached.FirstOrDefault(pushes) is { } wanted ? new ChosenNotification<T>(wanted, Push: true) : new ChosenNotification<T>(reached[0], Push: false);
    }

    public static IEnumerable<InboxItem> OnePerThing(IEnumerable<InboxItem> reached, Func<string, bool> pushes) =>
        reached.GroupBy(item => item.OneOf ?? item.Id)
            .Select(related => Choose(related.OrderBy(item => item.Rank), _ => true, item => pushes(item.Kind))!.Chosen);
}
