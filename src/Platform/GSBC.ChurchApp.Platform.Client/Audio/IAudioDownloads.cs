namespace GSBC.ChurchApp.Platform.Client.Audio;

public enum DownloadState
{
    Downloading,
    Done,
    Failed,
}

public sealed record AudioDownload(string Id, string Title, long Bytes, long Expected, DownloadState State)
{
    public double Fraction => State == DownloadState.Done ? 1 : Expected > 0 ? Math.Clamp((double)Bytes / Expected, 0, 1) : 0;
}

public interface IAudioDownloads
{
    bool IsAvailable { get; }

    event Action? Changed;

    Task<IReadOnlyDictionary<string, AudioDownload>> ListAsync();

    Task DownloadAsync(AudioItem item);

    Task RemoveAsync(string id);
}
