using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Client.Media;

public interface IFontLibrary
{
    bool MayChooseFromGoogle { get; }

    bool MayUploadFiles { get; }

    Task<IReadOnlyList<FontFamilyChoice>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task<CustomFont> ImportAsync(string family, CancellationToken cancellationToken = default);

    Task<CustomFont> UploadAsync(string fileName, long bytes, Stream content, CancellationToken cancellationToken = default);
}
