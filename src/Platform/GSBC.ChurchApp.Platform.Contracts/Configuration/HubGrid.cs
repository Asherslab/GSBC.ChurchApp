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
        columns = Math.Clamp(columns, FewestColumns, MostColumns);
        List<int> current = [.. Enumerable.Range(0, tiles.Count)];
        IReadOnlyList<TilePlace> before = Pack(columns, tiles);
        TilePlace own = before[dragged];
        TilePlace target = new(Math.Clamp(column, 0, columns - own.Columns) + 1, Math.Max(row, 0) + 1, own.Columns, own.Rows);
        HashSet<int> under = [.. current.Where(index => index != dragged && Overlaps(before[index], target))];
        List<int> best = current;
        (int, int, int, int, int) bestScore = Score(current);
        foreach (List<int> candidate in Candidates())
        {
            (int, int, int, int, int) score = Score(candidate);
            if (score.CompareTo(bestScore) < 0)
                (best, bestScore) = (candidate, score);
        }
        return bestScore.Item1 <= Covering ? best : current;

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
            yield return FlowedAround(columns, tiles, dragged, target, others, new HashSet<int>());
            yield return FlowedAround(columns, tiles, dragged, target, [.. others.Where(under.Contains), .. others.Where(index => !under.Contains(index))], under);
        }

        (int, int, int, int, int) Score(List<int> order)
        {
            IReadOnlyList<TilePlace> places = Pack(columns, order.Select(index => tiles[index]).ToList());
            TilePlace landed = places[order.IndexOf(dragged)];
            int landing = landed.Column == target.Column && landed.Row == target.Row ? Exact
                : Covers(landed, target.Column - 1, target.Row - 1) ? Covering
                : Elsewhere;
            int distance = Math.Abs(landed.Column - target.Column) + Math.Abs(landed.Row - target.Row);
            int outside = 0;
            int cascaded = 0;
            int moved = 0;
            for (int at = 0; at < order.Count; at++)
            {
                int index = order[at];
                if (index == dragged || places[at] with { Columns = 0, Rows = 0 } == before[index] with { Columns = 0, Rows = 0 })
                    continue;
                moved++;
                if (!under.Contains(index))
                    outside++;
                if (before[index].Row >= target.Row + target.Rows)
                    cascaded++;
            }
            return (landing, distance, outside, cascaded, moved);
        }
    }

    private const int Exact = 0;
    private const int Covering = 1;
    private const int Elsewhere = 2;

    private static List<int> FlowedAround(int columns, IReadOnlyList<TileSpan> tiles, int dragged, TilePlace pinned, IEnumerable<int> others, IReadOnlySet<int> displaced)
    {
        List<bool[]> taken = [];
        Take(taken, columns, pinned.Column - 1, pinned.Row - 1, pinned.Columns, pinned.Rows);
        List<(int Row, int Column, int Index)> placed = [(pinned.Row, pinned.Column, dragged)];
        foreach (int index in others)
        {
            int width = Math.Clamp(tiles[index].Columns, 1, columns);
            int height = Math.Clamp(tiles[index].Rows, 1, MostRows);
            (int column, int row) = FirstGap(taken, columns, width, height, displaced.Contains(index) ? pinned.Row - 1 : 0);
            Take(taken, columns, column, row, width, height);
            placed.Add((row + 1, column + 1, index));
        }
        return placed.OrderBy(place => place.Row).ThenBy(place => place.Column).Select(place => place.Index).ToList();
    }

    private static bool Overlaps(TilePlace place, TilePlace area) =>
        place.Column < area.Column + area.Columns && area.Column < place.Column + place.Columns
        && place.Row < area.Row + area.Rows && area.Row < place.Row + place.Rows;

    private static bool Covers(TilePlace place, int column, int row) =>
        column >= place.Column - 1 && column < place.Column - 1 + place.Columns && row >= place.Row - 1 && row < place.Row - 1 + place.Rows;

    private static (int Column, int Row) FirstGap(List<bool[]> taken, int columns, int width, int height, int fromRow = 0)
    {
        for (int row = fromRow; ; row++)
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
