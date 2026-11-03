namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public readonly record struct TileSpan(int Columns, int Rows);

public readonly record struct TilePlace(int Column, int Row, int Columns, int Rows);

public static class HubGrid
{
    public const int FewestColumns = 2;
    public const int MostColumns = 4;
    public const int MostRows = 4;

    public static IReadOnlyList<TilePlace> Pack(int columns, IReadOnlyList<TileSpan> tiles)
    {
        columns = Math.Clamp(columns, FewestColumns, MostColumns);
        List<bool[]> taken = [];
        List<TilePlace> places = new(tiles.Count);
        foreach (TileSpan tile in tiles)
        {
            int width = Math.Clamp(tile.Columns, 1, columns);
            int height = Math.Clamp(tile.Rows, 1, MostRows);
            (int column, int row) = FirstGap(taken, columns, width, height);
            Take(taken, columns, column, row, width, height);
            places.Add(new TilePlace(column + 1, row + 1, width, height));
        }
        return places;
    }

    private static (int Column, int Row) FirstGap(List<bool[]> taken, int columns, int width, int height)
    {
        for (int row = 0; ; row++)
        {
            for (int column = 0; column + width <= columns; column++)
            {
                if (Fits(taken, column, row, width, height))
                    return (column, row);
            }
        }
    }

    private static bool Fits(List<bool[]> taken, int column, int row, int width, int height)
    {
        for (int y = row; y < row + height && y < taken.Count; y++)
        {
            for (int x = column; x < column + width; x++)
            {
                if (taken[y][x])
                    return false;
            }
        }
        return true;
    }

    private static void Take(List<bool[]> taken, int columns, int column, int row, int width, int height)
    {
        while (taken.Count < row + height)
            taken.Add(new bool[columns]);
        for (int y = row; y < row + height; y++)
        {
            for (int x = column; x < column + width; x++)
                taken[y][x] = true;
        }
    }
}
