using GSBC.ChurchApp.Platform.Contracts.Media;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace GSBC.ChurchApp.Platform.Client.Uploads;

public sealed record PickedFile(MediaRef? Media, string? Problem);

public sealed class FileUploads(IFileShelf shelf, IServiceProvider services)
{
    private readonly Dictionary<string, StagedFile> _kept = [];

    private IUploadRequests? Requests => services.GetService<IUploadRequests>();

    public bool CanQueue => shelf.IsAvailable && Requests is not null;

    public string Accepted(PickableFiles kinds) => Requests?.Accepted(kinds) ?? "";

    public string? ContentTypeOf(string fileName, string reportedType, PickableFiles kinds) => Requests?.ContentTypeOf(fileName, reportedType, kinds);

    public string Unusable(string fileName, PickableFiles kinds) => Requests?.Unusable(fileName, kinds) ?? $"{fileName} can't be sent from here.";

    public async Task<PickedFile> KeepAsync(ElementReference input, string fileName, string reportedType, PickableFiles kinds, string altText = "", int index = 0)
    {
        if (Requests?.ContentTypeOf(fileName, reportedType, kinds) is not { } contentType)
            return new PickedFile(null, Unusable(fileName, kinds));

        StagedFile? kept;
        try
        {
            kept = await shelf.KeepAsync(input, contentType, index);
        }
        catch (JSException exception)
        {
            return new PickedFile(null, $"{fileName} couldn't be kept on this phone: {exception.Message}");
        }
        if (kept is null)
            return new PickedFile(null, $"{fileName} couldn't be kept on this phone. Try choosing it again.");

        _kept[kept.Preview] = kept;
        return new PickedFile(Placeholder(kept, altText), null);
    }

    public async Task<MediaRef?> KeepStoreCopyAsync(string contentType)
    {
        StagedFile? kept;
        try
        {
            kept = await shelf.CopyStoreAsync(contentType);
        }
        catch (JSException)
        {
            return null;
        }
        if (kept is null)
            return null;
        _kept[kept.Preview] = kept;
        return Placeholder(kept, "");
    }

    public string? HashOf(MediaRef media) => media.Url is { } preview && _kept.TryGetValue(preview, out StagedFile? file) && file.Sha256.Length > 0 ? file.Sha256 : null;

    public static bool IsWaiting(MediaRef? media) => media is { Id.Length: 0, Key.Length: 0, Url.Length: > 0 };

    public QueuedUpload? For(MediaRef? media, MediaSlot slot, UploadTarget? target = null) =>
        IsWaiting(media) && Requests is { } requests ? requests.For(KeptFile(media!), target ?? new UploadTarget(media!.AltText), [slot]) : null;

    private StagedFile KeptFile(MediaRef media) =>
        _kept.TryGetValue(media.Url!, out StagedFile? file)
            ? file
            : new StagedFile(Uri.UnescapeDataString(media.Url!.Split('/', '?')[^1]), media.FileName, media.ContentType, media.Bytes, "", media.Url);

    public async ValueTask ForgetAsync(MediaRef? media)
    {
        if (media is not { Id.Length: 0, Url: { } preview } || !_kept.Remove(preview, out StagedFile? file))
            return;
        await shelf.ForgetAsync(file.Token);
    }

    private static MediaRef Placeholder(StagedFile file, string altText) => new()
    {
        ContentType = file.ContentType,
        Bytes = file.Bytes,
        Url = file.Preview,
        FileName = file.Name,
        AltText = altText,
    };
}
