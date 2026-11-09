using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Platform.Client.Media;

public interface IFontLibrary
{
    bool MayChooseFromGoogle { get; }

    bool MayChooseFiles { get; }

    Task<IReadOnlyList<FontFamilyChoice>> SearchAsync(string query, CancellationToken cancellationToken = default);

    Task<CustomFont> ImportAsync(string family, CancellationToken cancellationToken = default);
}
