namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public readonly record struct TileSpan(int Columns, int Rows)
{
    public string Words => $"{Columns} wide, {Rows} tall";
}

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

    public static IReadOnlyList<int> Plan(int columns, IReadOnlyList<TileSpan> tiles, int dragged, int column, int row)
    {
        List<int> current = [.. Enumerable.Range(0, tiles.Count)];
        IReadOnlyList<TilePlace> before = Pack(columns, tiles);
        HashSet<int> under = [.. current.Where(index => index != dragged && Covers(before[index], column, row))];
        List<int> best = current;
        (int, int, int, int) bestScore = Score(current);
        foreach (List<int> candidate in Candidates())
        {
            (int, int, int, int) score = Score(candidate);
            if (score.CompareTo(bestScore) < 0)
                (best, bestScore) = (candidate, score);
        }
        return bestScore.Item1 == 0 ? best : current;

        IEnumerable<List<int>> Candidates()
        {
            foreach (int other in under)
            {
                List<int> swapped = [.. current];
                (swapped[dragged], swapped[other]) = (swapped[other], swapped[dragged]);
                yield return swapped;
            }
            List<int> others = current.Where(index => index != dragged).ToList();
            for (int at = 0; at <= others.Count; at++)
                yield return [.. others.Take(at), dragged, .. others.Skip(at)];
        }

        (int, int, int, int) Score(List<int> order)
        {
            IReadOnlyList<TilePlace> places = Pack(columns, order.Select(index => tiles[index]).ToList());
            TilePlace own = places[order.IndexOf(dragged)];
            bool covers = Covers(own, column, row);
            int distance = covers ? 0 : Math.Abs(own.Column - 1 - column) + Math.Abs(own.Row - 1 - row);
            int outside = 0;
            int moved = 0;
            for (int at = 0; at < order.Count; at++)
            {
                int index = order[at];
                if (index == dragged || places[at] with { Columns = 0, Rows = 0 } == before[index] with { Columns = 0, Rows = 0 })
                    continue;
                moved++;
                if (!under.Contains(index))
                    outside++;
            }
            return (covers ? 0 : 1, distance, outside, moved);
        }
    }

    private static bool Covers(TilePlace place, int column, int row) =>
        column >= place.Column - 1 && column < place.Column - 1 + place.Columns && row >= place.Row - 1 && row < place.Row - 1 + place.Rows;

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
