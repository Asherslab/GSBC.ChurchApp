using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Client.Media;

public interface IMediaFiles
{
    ValueTask<string?> SourceOfAsync(MediaRef media);

    ValueTask ForgetPrivateAsync();
}

public interface IPrivateMediaLinks
{
    ValueTask<string?> LinkAsync(string mediaId);
}
