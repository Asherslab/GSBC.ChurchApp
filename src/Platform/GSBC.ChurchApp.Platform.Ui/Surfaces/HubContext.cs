using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Ui.Surfaces;

public sealed record HubContext(bool Live, int Columns, IReadOnlyDictionary<string, string> Icons)
{
    public static readonly HubContext Still = new(false, HubGrid.FewestColumns, new Dictionary<string, string>());
}

public sealed record HubCell(ArrangedSurface Entry, TilePlace Place, int Index);

public enum TileShape
{
    Square,
    Row,
    Poster,
    Picture,
    Behind,
    Beside,
}
