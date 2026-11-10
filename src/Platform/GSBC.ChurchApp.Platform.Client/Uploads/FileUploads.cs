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

    public string Accepted(bool imagesOnly) => Requests?.Accepted(imagesOnly) ?? "";

    public async Task<PickedFile> KeepAsync(ElementReference input, string fileName, string reportedType, bool imagesOnly, string altText = "")
    {
        if (Requests?.ContentTypeOf(fileName, reportedType, imagesOnly) is not { } contentType)
            return new PickedFile(null, Requests?.Unusable(fileName, imagesOnly) ?? $"{fileName} can't be sent from this app.");

        StagedFile? kept;
        try
        {
            kept = await shelf.KeepAsync(input, contentType);
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
