namespace GSBC.ChurchApp.Platform.Client.Uploads;

public sealed record StagedFile(string Token, string Name, string ContentType, long Bytes, string Sha256, string Preview);

public sealed record UploadTarget(string AltText = "", string? PrivateTo = null, string? PrivateToScope = null, string? Area = null, string? AreaScopeId = null)
{
    public static readonly UploadTarget Public = new();
}

public sealed record MediaSlot(IReadOnlyList<int> Path)
{
    private const int MediaRefIdField = 1;

    public static MediaSlot Ref(params int[] fieldPath) => new([.. fieldPath, MediaRefIdField]);

    public static MediaSlot Id(params int[] fieldPath) => new(fieldPath);

    public string Text => string.Join('.', Path);
}

public enum PickableFiles
{
    Pictures,
    PicturesAndDocuments,
    PicturesAndVideos,
}

public sealed record QueuedCall(string Method, byte[] Request);

public sealed record QueuedUpload(StagedFile File, QueuedCall Find, QueuedCall Begin, QueuedCall Complete, IReadOnlyList<MediaSlot> Slots);

public interface IUploadRequests
{
    string Accepted(PickableFiles kinds);

    string? ContentTypeOf(string fileName, string reported, PickableFiles kinds);

    string Unusable(string fileName, PickableFiles kinds);

    QueuedUpload For(StagedFile file, UploadTarget target, IReadOnlyList<MediaSlot> slots);
}
