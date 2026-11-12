namespace GSBC.ChurchApp.Platform.Client.Modules;

public interface INavCounts
{
    IReadOnlyCollection<string> Feeds { get; }

    Task<IReadOnlyDictionary<string, int>> CountAsync();
}
