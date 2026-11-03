using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace GSBC.ChurchApp.Platform.Ui;

public sealed class SvgLabel : ComponentBase
{
    [Parameter, EditorRequired] public required string X { get; set; }
    [Parameter, EditorRequired] public required string Y { get; set; }
    [Parameter] public string Anchor { get; set; } = "start";
    [Parameter] public string? Class { get; set; }
    [Parameter, EditorRequired] public required string Text { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "text");
        builder.AddAttribute(1, "class", Class);
        builder.AddAttribute(2, "x", X);
        builder.AddAttribute(3, "y", Y);
        builder.AddAttribute(4, "text-anchor", Anchor);
        builder.AddContent(5, Text);
        builder.CloseElement();
    }
}
