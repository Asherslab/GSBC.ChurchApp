using GSBC.ChurchApp.Platform.Contracts.Content;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class MarkdownTableTests
{
    [Fact]
    public void ATableReadsBackAsItWasWrittenAfterRowsAndColumnsChange()
    {
        MarkdownTable table = MarkdownTable.Read("| Service | Time |\n| :--- | ---: |\n| Morning | 10:30am |\n| Evening")!;

        Assert.Equal([TableAlignment.Left, TableAlignment.Right], table.Alignments);
        Assert.Equal(["Evening", ""], table.Rows[2]);

        table.AddColumn();
        table.Rows[1][2] = "Main hall";
        table.AddRow();
        table.RemoveRow(2);
        table.RemoveColumn(1);

        Assert.Equal("| Service | Heading |\n| :--- | --- |\n| Morning | Main hall |\n|  |  |", table.Markdown());
        Assert.Equal(table.Markdown(), MarkdownTable.Read(table.Markdown())!.Markdown());
    }

    [Fact]
    public void APipeOrLineBreakTypedInACellNeverBreaksTheTable()
    {
        MarkdownTable table = MarkdownTable.New();
        table.Rows[1][0] = "Bring a plate | a drink";
        table.Rows[1][1] = "Two\nlines";

        TableBlock block = Assert.IsType<TableBlock>(Assert.Single(RichTextParser.Parse(table.Markdown()).Blocks));

        Assert.Equal(2, Assert.Single(block.Rows).Cells.Count);
        Assert.Equal(["Bring a plate | a drink", "Two lines"], MarkdownTable.Read(table.Markdown())!.Rows[1]);
    }

    [Fact]
    public void TheHeadingRowAndTheLastRowOrColumnStay()
    {
        MarkdownTable table = MarkdownTable.New();

        table.RemoveRow(0);
        table.RemoveRow(1);
        table.RemoveColumn(0);
        table.RemoveColumn(0);

        Assert.Equal((2, 1), (table.Rows.Count, table.Columns));
    }
}
