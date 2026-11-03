using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace GSBC.ChurchApp.Platform.Contracts.Configuration;

public static partial class SvgIcons
{
    public const int MostCharactersIn = 200_000;
    public const int MostCharactersKept = 40_000;
    private const string CurrentColour = "currentColor";
    private static readonly XNamespace Svg = "http://www.w3.org/2000/svg";

    private static readonly HashSet<string> Elements =
    [
        "svg", "g", "path", "circle", "ellipse", "line", "polyline", "polygon", "rect",
        "defs", "clipPath", "mask", "linearGradient", "radialGradient", "stop",
    ];

    private static readonly HashSet<string> Attributes =
    [
        "viewBox", "d", "cx", "cy", "r", "rx", "ry", "x", "y", "x1", "y1", "x2", "y2", "width", "height", "points",
        "fill", "fill-rule", "fill-opacity", "stroke", "stroke-width", "stroke-linecap", "stroke-linejoin",
        "stroke-miterlimit", "stroke-dasharray", "stroke-dashoffset", "stroke-opacity", "opacity", "clip-rule",
        "transform", "id", "clip-path", "mask", "offset", "stop-color", "stop-opacity", "gradientUnits",
        "gradientTransform", "fx", "fy", "fr", "spreadMethod", "maskUnits", "clipPathUnits", "vector-effect",
    ];

    private static readonly HashSet<string> Paints = ["fill", "stroke", "stop-color"];

    public static string? Sanitise(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MostCharactersIn)
            return null;

        XElement? root = Parse(text);
        if (root is null || root.Name.LocalName != "svg" || (root.Name.Namespace != Svg && root.Name.Namespace != XNamespace.None))
            return null;
        XNamespace source = root.Name.Namespace;

        string? viewBox = ViewBoxOf(root);
        if (viewBox is null)
            return null;

        XElement clean = new(Svg + "svg", new XAttribute("viewBox", viewBox));
        foreach (XAttribute attribute in root.Attributes().Where(attribute => attribute.Name.LocalName is not ("viewBox" or "width" or "height")))
            CopyIfSafe(attribute, clean);
        foreach (XElement child in root.Elements())
            CopyIfSafe(child, clean, source);

        if (!clean.Descendants().Any(element => element.Name.LocalName is not ("defs" or "g" or "stop" or "linearGradient" or "radialGradient" or "clipPath" or "mask")))
            return null;

        string markup = clean.ToString(SaveOptions.DisableFormatting);
        return markup.Length <= MostCharactersKept ? markup : null;
    }

    public static string Monochrome(string sanitised)
    {
        XElement? root = Parse(sanitised);
        if (root is null)
            return "";
        foreach (XElement element in root.DescendantsAndSelf())
        {
            foreach (XAttribute paint in element.Attributes().Where(attribute => Paints.Contains(attribute.Name.LocalName)).ToList())
            {
                if (!paint.Value.Trim().Equals("none", StringComparison.OrdinalIgnoreCase) && !paint.Value.StartsWith("url(", StringComparison.Ordinal))
                    paint.Value = CurrentColour;
            }
        }
        if (root.Attribute("fill") is null)
            root.SetAttributeValue("fill", CurrentColour);
        return root.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement? Parse(string text)
    {
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            MaxCharactersInDocument = MostCharactersIn,
        };
        try
        {
            using StringReader reader = new(text.Trim());
            using XmlReader xml = XmlReader.Create(reader, settings);
            return XDocument.Load(xml).Root;
        }
        catch (XmlException)
        {
            return null;
        }
    }

    private static string? ViewBoxOf(XElement root)
    {
        if (root.Attribute("viewBox")?.Value is { } given)
        {
            string[] parts = Numbers().Split(given.Trim());
            return parts.Length == 4 && parts.All(IsNumber) && Number(parts[2]) > 0 && Number(parts[3]) > 0 ? string.Join(' ', parts) : null;
        }
        string? width = Length(root.Attribute("width")?.Value);
        string? height = Length(root.Attribute("height")?.Value);
        return width is not null && height is not null ? $"0 0 {width} {height}" : null;
    }

    private static string? Length(string? text)
    {
        string trimmed = (text ?? "").Trim();
        if (trimmed.EndsWith("px", StringComparison.Ordinal))
            trimmed = trimmed[..^2];
        return IsNumber(trimmed) && Number(trimmed) > 0 ? trimmed : null;
    }

    private static bool IsNumber(string text) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && double.IsFinite(value);

    private static double Number(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static void CopyIfSafe(XElement source, XElement into, XNamespace from)
    {
        if (source.Name.Namespace != from || !Elements.Contains(source.Name.LocalName) || source.Name.LocalName == "svg")
            return;

        XElement copy = new(Svg + source.Name.LocalName);
        foreach (XAttribute attribute in source.Attributes())
            CopyIfSafe(attribute, copy);
        foreach (XElement child in source.Elements())
            CopyIfSafe(child, copy, from);
        into.Add(copy);
    }

    private static void CopyIfSafe(XAttribute attribute, XElement into)
    {
        if (attribute.IsNamespaceDeclaration || attribute.Name.Namespace != XNamespace.None || !Attributes.Contains(attribute.Name.LocalName))
            return;
        string value = attribute.Value.Trim();
        if (value.Length == 0 || value.Length > 20_000 || !SafeValue(attribute.Name.LocalName, value))
            return;
        into.SetAttributeValue(attribute.Name.LocalName, value);
    }

    private static bool SafeValue(string name, string value)
    {
        if (value.Contains('<') || value.Contains('>') || value.Contains('\\') || value.Contains(':') && !value.StartsWith("url(#", StringComparison.Ordinal))
            return false;
        if (value.Contains("url(", StringComparison.OrdinalIgnoreCase))
            return InternalReference().IsMatch(value);
        if (name == "id")
            return Identifier().IsMatch(value);
        return !value.Contains("expression", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"[\s,]+")]
    private static partial Regex Numbers();

    [GeneratedRegex(@"^url\(#[A-Za-z][\w.-]*\)$")]
    private static partial Regex InternalReference();

    [GeneratedRegex(@"^[A-Za-z][\w.-]*$")]
    private static partial Regex Identifier();
}
