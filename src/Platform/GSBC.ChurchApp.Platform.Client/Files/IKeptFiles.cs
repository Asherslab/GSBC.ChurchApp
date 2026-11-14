namespace GSBC.ChurchApp.Platform.Client.Files;

public enum KeptFileState
{
    Downloading,
    Done,
    Failed,
}

public sealed record KeptFile(string Id, string Url, string Title, long Bytes, long Expected, KeptFileState State)
{
    public double Fraction => State == KeptFileState.Done ? 1 : Expected > 0 ? Math.Clamp((double)Bytes / Expected, 0, 1) : 0;
}

public interface IKeptFiles
{
    bool IsAvailable { get; }

    event Action? Changed;

    Task KeepAsync(string id, string url, string title);

    Task<IReadOnlyList<KeptFile>> ListAsync();

    Task RemoveAsync(string id);

    Task<byte[]> ReadAsync(string id, long offset = 0, long length = -1);
}
