using System.Buffers;
using System.Text;
using System.Text.Json;

namespace GSBC.ChurchApp.Platform.Ui.Maps;

public enum MapSketchTool
{
    None,
    Polygon,
    Lasso,
    Line,
    Brush,
    Pin,
}

public sealed record MapSketchStroke(MapSketchTool Tool, IReadOnlyList<MapPoint> Points);

public enum MapLabelStyle
{
    Ring,
    Filled,
}

public sealed record MapLabel(MapPoint Point, string Text, string Colour, int Order = 0, MapLabelStyle Style = MapLabelStyle.Ring, string? Short = null);

public static class MapSketchTools
{
    public static string Name(MapSketchTool tool) => tool switch
    {
        MapSketchTool.Polygon => "polygon",
        MapSketchTool.Lasso => "lasso",
        MapSketchTool.Line => "line",
        MapSketchTool.Brush => "brush",
        MapSketchTool.Pin => "pin",
        _ => "none",
    };

    public static MapSketchTool Of(string name) => name switch
    {
        "polygon" => MapSketchTool.Polygon,
        "lasso" => MapSketchTool.Lasso,
        "line" => MapSketchTool.Line,
        "brush" => MapSketchTool.Brush,
        "pin" => MapSketchTool.Pin,
        _ => MapSketchTool.None,
    };
}

public static class MapSketchJson
{
    public static IReadOnlyList<MapPoint> Points(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray().Select(point => new MapPoint(point[1].GetDouble(), point[0].GetDouble())).ToList();
    }

    public static string Labels(IEnumerable<MapLabel> labels) => Write(json =>
    {
        json.WriteStartArray();
        foreach (MapLabel label in labels)
        {
            json.WriteStartObject();
            json.WriteString("text", label.Text);
            json.WriteString("colour", label.Colour);
            json.WriteNumber("order", label.Order);
            json.WriteBoolean("filled", label.Style == MapLabelStyle.Filled);
            json.WriteString("short", label.Short ?? label.Text);
            json.WritePropertyName("point");
            Point(json, label.Point);
            json.WriteEndObject();
        }
        json.WriteEndArray();
    });

    public static string Preview(IEnumerable<MapLine> lines) => Write(json =>
    {
        json.WriteStartArray();
        foreach (MapLine line in lines.Where(line => line.Points.Count >= 2))
        {
            json.WriteStartObject();
            if (line.Colour is not null)
                json.WriteString("colour", line.Colour);
            json.WritePropertyName("points");
            json.WriteStartArray();
            foreach (MapPoint point in line.Points)
                Point(json, point);
            json.WriteEndArray();
            json.WriteEndObject();
        }
        json.WriteEndArray();
    });

    private static void Point(Utf8JsonWriter json, MapPoint point)
    {
        json.WriteStartArray();
        json.WriteNumberValue(Math.Round(point.Lon, 6));
        json.WriteNumberValue(Math.Round(point.Lat, 6));
        json.WriteEndArray();
    }

    private static string Write(Action<Utf8JsonWriter> write)
    {
        ArrayBufferWriter<byte> buffer = new();
        using (Utf8JsonWriter json = new(buffer))
            write(json);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}
