namespace GSBC.ChurchApp.Platform.Client.Modules;

public interface IPinnedPages
{
    IReadOnlyCollection<string> Feeds { get; }

    Task<IReadOnlyList<string>> LoadAsync();

    Task SaveAsync(IReadOnlyList<string> hrefs);
}
