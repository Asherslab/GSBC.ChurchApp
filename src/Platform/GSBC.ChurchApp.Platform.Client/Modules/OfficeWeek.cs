namespace GSBC.ChurchApp.Platform.Client.Modules;

public interface IOfficeWeekSource
{
    IReadOnlyCollection<string> Feeds { get; }

    Task<OfficeWeek> LoadAsync(DateTime from, DateTime until);
}

public sealed record OfficeWeek(IReadOnlyList<OfficeTask> Tasks, IReadOnlyList<OfficeSpan> Spans)
{
    public static OfficeWeek Nothing { get; } = new([], []);
}

public sealed record OfficeTask(string Title, string Detail, string Href, string Action, DateTime Since);

public sealed record OfficeSpan(string Title, DateTime StartsAt, DateTime EndsAt, string? Href, OfficeSpanKind Kind);

public enum OfficeSpanKind
{
    Everyone,
    Layer,
    Draft,
}
