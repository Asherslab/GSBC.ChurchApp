using System.Text;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public sealed class MarkdownTable
{
    private const string NewHeading = "Heading";

    private MarkdownTable(List<TableAlignment> alignments, List<List<string>> rows)
    {
        Alignments = alignments;
        Rows = rows;
    }

    public List<TableAlignment> Alignments { get; }

    public List<List<string>> Rows { get; }

    public int Columns => Alignments.Count;

    public static MarkdownTable New() => new([TableAlignment.None, TableAlignment.None], [[NewHeading, NewHeading], ["", ""]]);

    public static MarkdownTable? Read(string markdown)
    {
        string[] lines = markdown.Replace("\r\n", "\n").Trim().Split('\n');
        if (lines.Length < 2 || Cells(lines[1]) is not { Count: > 0 } delimiters || !delimiters.All(IsDelimiter))
            return null;
        List<TableAlignment> alignments = delimiters.Select(AlignmentOf).ToList();
        List<List<string>> rows = [Fitted(Cells(lines[0]), alignments.Count)];
        rows.AddRange(lines.Skip(2).Where(line => line.Trim().Length > 0).Select(line => Fitted(Cells(line), alignments.Count)));
        return new MarkdownTable(alignments, rows);
    }

    public void AddRow() => Rows.Add([.. Enumerable.Repeat("", Columns)]);

    public void AddColumn()
    {
        Alignments.Add(TableAlignment.None);
        Rows[0].Add(NewHeading);
        foreach (List<string> row in Rows.Skip(1))
            row.Add("");
    }

    public void RemoveRow(int index)
    {
        if (index > 0 && index < Rows.Count && Rows.Count > 2)
            Rows.RemoveAt(index);
    }

    public void RemoveColumn(int index)
    {
        if (index < 0 || index >= Columns || Columns < 2)
            return;
        Alignments.RemoveAt(index);
        foreach (List<string> row in Rows)
            row.RemoveAt(index);
    }

    public string Markdown()
    {
        StringBuilder text = new();
        text.Append(Line(Rows[0])).Append('\n').Append(Line(Alignments.Select(Delimiter)));
        foreach (List<string> row in Rows.Skip(1))
            text.Append('\n').Append(Line(row));
        return text.ToString();
    }

    private static string Line(IEnumerable<string> cells) => "| " + string.Join(" | ", cells.Select(Written)) + " |";

    private static string Written(string cell)
    {
        string flat = string.Join(' ', cell.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();
        return flat.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string Delimiter(TableAlignment alignment) => alignment switch
    {
        TableAlignment.Left => ":---",
        TableAlignment.Centre => ":---:",
        TableAlignment.Right => "---:",
        _ => "---",
    };

    private static List<string> Cells(string line)
    {
        string trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
            trimmed = trimmed[1..];
        if (trimmed.EndsWith('|') && !trimmed.EndsWith("\\|", StringComparison.Ordinal))
            trimmed = trimmed[..^1];
        List<string> cells = [];
        StringBuilder cell = new();
        for (int index = 0; index < trimmed.Length; index++)
        {
            if (trimmed[index] == '|' && (index == 0 || trimmed[index - 1] != '\\'))
            {
                cells.Add(Plain(cell));
                cell.Clear();
                continue;
            }
            cell.Append(trimmed[index]);
        }
        cells.Add(Plain(cell));
        return cells;
    }

    private static string Plain(StringBuilder cell) => cell.ToString().Trim().Replace("\\|", "|", StringComparison.Ordinal);

    private static List<string> Fitted(List<string> cells, int columns) =>
        [.. cells.Take(columns), .. Enumerable.Repeat("", Math.Max(0, columns - cells.Count))];

    private static bool IsDelimiter(string cell) => cell.Trim(':').Length > 0 && cell.Trim(':').All(character => character == '-');

    private static TableAlignment AlignmentOf(string cell) => (cell.StartsWith(':'), cell.EndsWith(':')) switch
    {
        (true, true) => TableAlignment.Centre,
        (true, false) => TableAlignment.Left,
        (false, true) => TableAlignment.Right,
        _ => TableAlignment.None,
    };
}
