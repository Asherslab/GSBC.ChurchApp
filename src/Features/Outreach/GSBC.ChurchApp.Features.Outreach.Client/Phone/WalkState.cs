using System.Text.Json.Serialization;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Features.Outreach.Routing;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed class SavedSide
{
    public int Street { get; set; }
    public int Side { get; set; }
    public double From { get; set; }
    public double To { get; set; }

    public SideStretch ToStretch() => new(Street, (Side)Side, From, To);

    public static SavedSide Of(SideStretch side) => new() { Street = side.Street, Side = (int)side.Side, From = side.From, To = side.To };
}

public sealed class SavedMarker
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public int Kind { get; set; }
    public string Label { get; set; } = "";
}

public sealed class SavedStep
{
    public string Street { get; set; } = "";
    public string Title { get; set; } = "";
    public bool Delivers { get; set; }
    public int Letterboxes { get; set; }
    public int Metres { get; set; }
    public int Seconds { get; set; }
    public List<double[]> Line { get; set; } = [];
    public List<SavedSide> Sides { get; set; } = [];
    public List<SavedMarker> Markers { get; set; } = [];
}

public sealed class WalkState
{
    public string WalkId { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public List<string> Campaigns { get; set; } = [];
    public WalkMode Mode { get; set; }
    public string DayId { get; set; } = "";
    public string RouteId { get; set; } = "";
    public string Name { get; set; } = "";
    public string RegionId { get; set; } = "";
    public long RegionVersion { get; set; }
    public int PlannedMetres { get; set; }
    public int PlannedLetterboxes { get; set; }
    public int PlannedSeconds { get; set; }
    public int Bundles { get; set; }
    public List<SavedStep> Steps { get; set; } = [];
    public long FixFrom { get; set; }
    public long FixLatest { get; set; }
    public int Seq { get; set; }
    public HashSet<string> Sent { get; set; } = [];
    public int SentMetres { get; set; }
    public int Conversations { get; set; }
    public int SentConversations { get; set; }
    public int Doors { get; set; }
    public int SentDoors { get; set; }
    public bool Paused { get; set; }
    public bool Tracking { get; set; }
    public bool WithPartner { get; set; }
    public bool Finished { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime LastSentAt { get; set; }
    public List<SavedSide> Skipped { get; set; } = [];
    public List<string> Marked { get; set; } = [];

    public static List<SavedStep> StepsOf(RouteSummary summary) =>
    [
        .. summary.Steps.Select(step => new SavedStep
        {
            Street = step.Street,
            Title = step.Title,
            Delivers = step.Delivers,
            Letterboxes = step.Letterboxes,
            Metres = (int)Math.Round(step.Metres),
            Seconds = (int)Math.Round(step.Time.TotalSeconds),
            Line = [.. step.Line.Select(point => new[] { Math.Round(point.Lat, 6), Math.Round(point.Lon, 6) })],
            Sides = [.. step.Sides.Select(SavedSide.Of)],
            Markers = [.. step.Markers.Select(marker => new SavedMarker { Lat = marker.Point.Lat, Lon = marker.Point.Lon, Kind = (int)marker.Kind, Label = marker.Label })],
        }),
    ];
}

public sealed class WalkHistoryEntry
{
    public string WalkId { get; set; } = "";
    public DateTime StartedAt { get; set; }
    public DateTime EndedAt { get; set; }
    public int Metres { get; set; }
    public int Letterboxes { get; set; }
    public int Conversations { get; set; }
    public int Doors { get; set; }
    public WalkMode Mode { get; set; }
    public List<string> Campaigns { get; set; } = [];
    public string Name { get; set; } = "";
}

[JsonSerializable(typeof(WalkState))]
[JsonSerializable(typeof(List<WalkHistoryEntry>))]
internal sealed partial class WalkJson : JsonSerializerContext;
