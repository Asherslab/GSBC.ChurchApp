using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Platform.Contracts.Content;

public sealed record PastedNode(
    [property: JsonPropertyName("k")] string Kind,
    [property: JsonPropertyName("t")] string? Text = null,
    [property: JsonPropertyName("h")] string? Href = null,
    [property: JsonPropertyName("c")] IReadOnlyList<PastedNode>? Children = null)
{
    public IReadOnlyList<PastedNode> Inner => Children ?? [];
}

public static partial class PastedText
{
    private static readonly HashSet<string> BlockKinds = ["p", "h", "li", "ul", "ol", "q"];

    public static string FromJson(string json, bool headings) =>
        ContentJson.TryRead(json, out PastedNode? root) && root is not null ? ToMarkdown(root, headings) : "";

    public static string ToMarkdown(PastedNode root, bool headings)
    {
        List<string> blocks = [];
        Blocks(root, headings, blocks, quoted: false);
        return string.Join("\n\n", MergeLists(blocks)).Trim('\n');
    }

    private static void Blocks(PastedNode node, bool headings, List<string> blocks, bool quoted)
    {
        List<PastedNode> run = [];
        foreach (PastedNode child in node.Inner)
        {
            if (!BlockKinds.Contains(child.Kind))
            {
                run.Add(child);
                continue;
            }
            Flush();
            switch (child.Kind)
            {
                case "ul" or "ol":
                    blocks.Add(List(child));
                    break;
                case "li":
                    blocks.Add(ListLine(child, ordered: Marker(child) is { } marker && OrderedMarker().IsMatch(marker), number: 1));
                    break;
                case "q":
                    List<string> inner = [];
                    Blocks(child, headings, inner, quoted: true);
                    blocks.AddRange(inner);
                    break;
                case "h":
                    Add(Inline(child.Inner), headings ? "## " : null, bold: !headings);
                    break;
                default:
                    if (child.Inner.Any(grandchild => BlockKinds.Contains(grandchild.Kind)))
                        Blocks(child, headings, blocks, quoted);
                    else
                        Add(Inline(child.Inner), null, bold: false);
                    break;
            }
        }
        Flush();

        void Flush()
        {
            if (run.Count == 0)
                return;
            Add(Inline(run), null, bold: false);
            run = [];
        }

        void Add(string text, string? prefix, bool bold)
        {
            string trimmed = text.Trim();
            if (trimmed.Length == 0)
                return;
            string line = bold ? $"**{trimmed}**" : prefix is null ? string.Join('\n', trimmed.Split('\n').Select(LiteralStart)) : trimmed;
            string block = prefix + line;
            blocks.Add(quoted ? string.Join('\n', block.Split('\n').Select(part => "> " + part)) : block);
        }
    }

    private static string List(PastedNode list)
    {
        int number = 1;
        return string.Join('\n', list.Inner
            .Where(item => item.Kind == "li")
            .Select(item => ListLine(item, list.Kind == "ol", number++)));
    }

    private static string ListLine(PastedNode item, bool ordered, int number) =>
        (ordered ? $"{number}. " : "- ") + Inline(item.Inner.Where(child => child.Kind != "marker").ToList()).Replace('\n', ' ').Trim();

    private static IEnumerable<string> MergeLists(List<string> blocks)
    {
        for (int index = 0; index < blocks.Count; index++)
        {
            string block = blocks[index];
            while (index + 1 < blocks.Count && IsListLine(block) && IsListLine(blocks[index + 1]) && Ordered(block) == Ordered(blocks[index + 1]))
            {
                string next = blocks[++index];
                block += "\n" + (Ordered(next) ? $"{block.Split('\n').Length + 1}. {next[(next.IndexOf(". ", StringComparison.Ordinal) + 2)..]}" : next);
            }
            yield return block;
        }
    }

    private static bool IsListLine(string block) => block.StartsWith("- ", StringComparison.Ordinal) || OrderedLine().IsMatch(block);

    private static bool Ordered(string block) => OrderedLine().IsMatch(block);

    private static string LiteralStart(string line)
    {
        if (!StartsFormatting().IsMatch(line))
            return line;
        int digits = line.TakeWhile(char.IsAsciiDigit).Count();
        return digits > 0 ? line[..digits] + "\\" + line[digits..] : "\\" + line;
    }

    private static string? Marker(PastedNode node) => node.Inner.FirstOrDefault(child => child.Kind == "marker")?.Text;

    private static string Inline(IReadOnlyList<PastedNode> nodes)
    {
        StringBuilder text = new();
        foreach (PastedNode node in nodes)
            text.Append(InlineOf(node));
        return Spaces().Replace(text.ToString(), " ").Replace(" \n", "\n", StringComparison.Ordinal).Replace("\n ", "\n", StringComparison.Ordinal);
    }

    private static string InlineOf(PastedNode node) => node.Kind switch
    {
        "t" => Escaped(node.Text ?? ""),
        "br" => "\n",
        "marker" => "",
        "b" => Wrapped(Inline(node.Inner), "**"),
        "i" => Wrapped(Inline(node.Inner), "*"),
        "a" when node.Href is { } href && LinkTargets.IsSafe(href.Trim()) && Inline(node.Inner).Trim() is { Length: > 0 } label && !label.Contains('\n') =>
            $"[{label}]({href.Trim()})",
        _ => Inline(node.Inner),
    };

    private static string Wrapped(string inner, string marker)
    {
        string trimmed = inner.Trim();
        if (trimmed.Length == 0 || trimmed.Contains('\n') || trimmed.StartsWith(marker, StringComparison.Ordinal))
            return inner;
        string before = inner[..^inner.TrimStart().Length];
        string after = inner[inner.TrimEnd().Length..];
        return before + marker + trimmed + marker + after;
    }

    private static string Escaped(string text)
    {
        StringBuilder escaped = new(text.Length);
        foreach (char character in text.Replace(' ', ' ').Replace("\r", "", StringComparison.Ordinal).Replace('\n', ' '))
        {
            if (character is '*' or '_' or '[' or ']' or '\\' or '`')
                escaped.Append('\\');
            escaped.Append(character);
        }
        return escaped.ToString();
    }

    [GeneratedRegex(@"^(?:#{1,6} |>|[-+] |\d{1,3}[.)] )")]
    private static partial Regex StartsFormatting();

    [GeneratedRegex(@"[ \t]+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"^\d{1,3}[.)]")]
    private static partial Regex OrderedMarker();

    [GeneratedRegex(@"^\d{1,3}\. ")]
    private static partial Regex OrderedLine();
}
