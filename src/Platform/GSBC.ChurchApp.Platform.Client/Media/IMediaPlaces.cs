namespace GSBC.ChurchApp.Platform.Client.Media;

public interface IMediaPlaces
{
    Task<IReadOnlyDictionary<string, string>> FoldersOfAsync(IReadOnlyCollection<string> mediaIds);
}
