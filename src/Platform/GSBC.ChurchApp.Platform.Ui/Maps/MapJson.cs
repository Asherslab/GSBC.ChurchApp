using System.Buffers;
using System.Text;
using System.Text.Json;

namespace GSBC.ChurchApp.Platform.Ui.Maps;

public static class MapJson
{
    public static string Options(MapSource source, MapPoint? centre, double zoom, bool interactive, bool follow) => Write(json =>
    {
        json.WriteStartObject();
        json.WriteStartObject("source");
        json.WriteString("kind", source.Kind);
        json.WriteString(source.Kind == MapSource.FromKeptFile ? "id" : "url", source.Location);
        json.WriteString("attribution", source.Attribution);
        json.WriteEndObject();
        if (centre is { } point)
        {
            json.WritePropertyName("centre");
            Point(json, point);
        }
        json.WriteNumber("zoom", zoom);
        json.WriteBoolean("interactive", interactive);
        json.WriteBoolean("follow", follow);
        json.WriteEndObject();
    });

    public static string Lines(IEnumerable<MapLine> lines) => Write(json =>
    {
        json.WriteStartArray();
        foreach (MapLine line in lines.Where(line => line.Points.Count >= 2))
        {
            json.WriteStartObject();
            json.WriteString("style", StyleName(line.Style));
            Optional(json, "label", line.Label);
            Optional(json, "colour", line.Colour);
            Optional(json, "id", line.Id);
            if (line.Age is { } age)
                json.WriteNumber("age", Math.Round(Math.Clamp(age, 0, 1), 3));
            json.WritePropertyName("points");
            Points(json, line.Points);
            json.WriteEndObject();
        }
        json.WriteEndArray();
    });

    public static string Areas(IEnumerable<MapArea> areas) => Write(json =>
    {
        json.WriteStartArray();
        foreach (MapArea area in areas.Where(area => area.Points.Count >= 3))
        {
            json.WriteStartObject();
            Optional(json, "colour", area.Colour);
            Optional(json, "id", area.Id);
            if (area.Dashed)
                json.WriteBoolean("dashed", true);
            if (area.Outline)
                json.WriteBoolean("outline", true);
            json.WritePropertyName("points");
            Points(json, [.. area.Points, area.Points[0]]);
            json.WriteEndObject();
        }
        json.WriteEndArray();
    });

    public static string Markers(IEnumerable<MapMarker> markers) => Write(json =>
    {
        json.WriteStartArray();
        foreach (MapMarker marker in markers)
        {
            json.WriteStartObject();
            json.WriteString("kind", marker.Kind.ToString().ToLowerInvariant());
            Optional(json, "label", marker.Label);
            Optional(json, "id", marker.Id);
            json.WritePropertyName("point");
            Point(json, marker.Point);
            json.WriteEndObject();
        }
        json.WriteEndArray();
    });

    public static string? You(MapYou? you) => you is null ? null : Write(json =>
    {
        json.WriteStartObject();
        json.WritePropertyName("point");
        Point(json, you.Point);
        json.WriteNumber("accuracy", Math.Round(you.AccuracyMetres, 1));
        json.WriteEndObject();
    });

    public static string Frame(MapFrame frame) => Write(json =>
    {
        json.WriteStartObject();
        json.WriteNumber("south", frame.South);
        json.WriteNumber("west", frame.West);
        json.WriteNumber("north", frame.North);
        json.WriteNumber("east", frame.East);
        json.WriteNumber("padding", frame.Padding);
        json.WriteNumber("maxZoom", frame.MaxZoom);
        json.WriteEndObject();
    });

    private static string StyleName(MapLineStyle style) => style switch
    {
        MapLineStyle.WalkDone => "walk-done",
        MapLineStyle.LongWalk => "long",
        _ => style.ToString().ToLowerInvariant(),
    };

    private static void Optional(Utf8JsonWriter json, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            json.WriteString(name, value);
    }

    private static void Point(Utf8JsonWriter json, MapPoint point)
    {
        json.WriteStartArray();
        json.WriteNumberValue(Math.Round(point.Lon, 6));
        json.WriteNumberValue(Math.Round(point.Lat, 6));
        json.WriteEndArray();
    }

    private static void Points(Utf8JsonWriter json, IEnumerable<MapPoint> points)
    {
        json.WriteStartArray();
        foreach (MapPoint point in points)
            Point(json, point);
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
