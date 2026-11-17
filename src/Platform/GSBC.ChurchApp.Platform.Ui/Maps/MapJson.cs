using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace GSBC.ChurchApp.Platform.Ui.Maps;

public static class MapJson
{
    private const int LineHead = 3;

    public static string Options(MapSource source, MapPoint? centre, double zoom, bool interactive, bool follow, MapBackdrop? backdrop = null, bool heading = false) => Write(json =>
    {
        json.WriteStartObject();
        json.WriteStartObject("source");
        json.WriteString("kind", source.Kind);
        if (source.Kind == MapSource.FromUrls)
        {
            json.WriteStartArray("urls");
            foreach (string url in source.Locations)
                json.WriteStringValue(url);
            json.WriteEndArray();
        }
        else
            json.WriteString(source.Kind == MapSource.FromKeptFile ? "id" : "url", source.Location);
        if (source.Fallback is not null)
            json.WriteString("url", source.Fallback);
        json.WriteString("attribution", source.Attribution);
        json.WriteEndObject();
        if (backdrop is not null)
        {
            json.WriteStartObject("backdrop");
            json.WriteString("url", backdrop.Url);
            json.WritePropertyName("bounds");
            json.WriteRawValue(Frame(backdrop.Bounds));
            json.WriteNumber("maxZoom", backdrop.MaxZoom);
            json.WriteEndObject();
        }
        if (centre is { } point)
        {
            json.WritePropertyName("centre");
            Point(json, point);
        }
        json.WriteNumber("zoom", zoom);
        json.WriteBoolean("interactive", interactive);
        json.WriteBoolean("follow", follow);
        json.WriteBoolean("heading", heading);
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

    public static (string Kinds, byte[] Points) LineSet(IReadOnlyList<MapLine> lines)
    {
        List<MapLine> drawn = [.. lines.Where(line => line.Points.Count >= 2)];
        List<MapLine> kinds = [];
        Dictionary<(MapLineStyle, string?, string?, string?), int> kindOf = [];
        double[] values = new double[drawn.Sum(line => LineHead + line.Points.Count * 2)];
        int at = 0;
        foreach (MapLine line in drawn)
        {
            if (!kindOf.TryGetValue((line.Style, line.Label, line.Colour, line.Id), out int kind))
            {
                kindOf[(line.Style, line.Label, line.Colour, line.Id)] = kind = kinds.Count;
                kinds.Add(line);
            }
            values[at++] = kind;
            values[at++] = line.Age is { } age ? Math.Round(Math.Clamp(age, 0, 1), 3) : double.NaN;
            values[at++] = line.Points.Count;
            foreach (MapPoint point in line.Points)
            {
                values[at++] = Math.Round(point.Lon, 6);
                values[at++] = Math.Round(point.Lat, 6);
            }
        }
        string kindsJson = Write(json =>
        {
            json.WriteStartArray();
            foreach (MapLine kind in kinds)
            {
                json.WriteStartObject();
                json.WriteString("style", StyleName(kind.Style));
                Optional(json, "label", kind.Label);
                Optional(json, "colour", kind.Colour);
                Optional(json, "id", kind.Id);
                json.WriteEndObject();
            }
            json.WriteEndArray();
        });
        return (kindsJson, MemoryMarshal.AsBytes(values.AsSpan()).ToArray());
    }

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
            if (area.Hatched)
                json.WriteBoolean("hatched", true);
            json.WritePropertyName("points");
            Points(json, [.. area.Points, area.Points[0]]);
            if (area.Holes is { Count: > 0 } holes)
            {
                json.WriteStartArray("holes");
                foreach (IReadOnlyList<MapPoint> hole in holes)
                    Points(json, hole);
                json.WriteEndArray();
            }
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
            if (marker.Chosen)
                json.WriteBoolean("chosen", true);
            if (marker.Corner)
                json.WriteBoolean("corner", true);
            if (marker.ShownFrom > 0)
                json.WriteNumber("from", Math.Round(marker.ShownFrom, 2));
            if (marker.ShownUntil > 0)
                json.WriteNumber("until", Math.Round(marker.ShownUntil, 2));
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

    public static string? Reach(MapReach? reach) => reach is null ? null : Write(json =>
    {
        json.WriteStartObject();
        json.WritePropertyName("beyond");
        json.WriteStartArray();
        foreach (IReadOnlyList<IReadOnlyList<MapPoint>> polygon in reach.Beyond())
        {
            json.WriteStartArray();
            foreach (IReadOnlyList<MapPoint> ring in polygon)
                Points(json, ring);
            json.WriteEndArray();
        }
        json.WriteEndArray();
        json.WritePropertyName("covered");
        json.WriteStartArray();
        foreach (MapFrame box in reach.Covered)
            json.WriteRawValue(Frame(box));
        json.WriteEndArray();
        json.WritePropertyName("closed");
        json.WriteStartArray();
        foreach (IReadOnlyList<MapPoint> ring in reach.Closed())
            Points(json, ring);
        json.WriteEndArray();
        json.WritePropertyName("edge");
        json.WriteStartArray();
        foreach (IReadOnlyList<MapPoint> line in reach.Edge())
            Points(json, line);
        json.WriteEndArray();
        json.WritePropertyName("others");
        json.WriteRawValue(Areas(reach.Others));
        json.WritePropertyName("notes");
        json.WriteRawValue(Markers(reach.Notes));
        if (reach.Limit is { } limit)
        {
            json.WritePropertyName("limit");
            json.WriteRawValue(Frame(limit));
        }
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
        json.WriteNumber("start", frame.StartPadding);
        json.WriteNumber("below", frame.BottomPadding);
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
