namespace GSBC.ChurchApp.Platform.Api.Analytics;

public interface ISubjectNames
{
    string SubjectKind { get; }

    ValueTask<IReadOnlyDictionary<string, string>> NamesAsync(IReadOnlyCollection<string> subjects, CancellationToken cancellationToken);
}
