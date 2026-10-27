using System.Text.Json.Serialization;

namespace Gsbc.AppPoc.Data.Outreach;

public enum ExclusionReason { NonResidential, Complaint, WrongData, Unsafe, PrivateEstate, Other }

public enum ReportScope { Area, Street, Letterbox }

public enum ReportReason { NotHomes, Unsafe, Complaint, WrongOnMap, Other }

public enum ReportStatus { SentForReview, MadeZone, MadeAddress, Dismissed }

public sealed record PlannerPin(GeoPoint Point, string Kind);

public sealed record LatLon(double Lat, double Lon)
{
    public GeoPoint Point => new(Lat, Lon);

    public static LatLon From(GeoPoint point) => new(point.Lat, point.Lon);
}

public sealed record ExclusionZone(string Id, IReadOnlyList<LatLon> Polygon, ExclusionReason Reason, string Note, DateTime Added);

public sealed record ExcludedAddress(string Id, int Edge, int Side, LatLon Point, ExclusionReason Reason, string Note, bool ShowWalkers, DateTime Added);

public sealed record WalkerReport(
    string Id,
    ReportScope Scope,
    ReportReason Reason,
    string Note,
    string? Photo,
    LatLon? Point,
    int? Edge,
    string Where,
    DateTime Sent,
    ReportStatus Status);

public static class OutreachLabels
{
    public static readonly IReadOnlyList<ExclusionReason> ExclusionReasons =
        [ExclusionReason.NonResidential, ExclusionReason.Complaint, ExclusionReason.WrongData, ExclusionReason.Unsafe, ExclusionReason.PrivateEstate, ExclusionReason.Other];

    public static readonly IReadOnlyList<ReportReason> ReportReasons =
        [ReportReason.NotHomes, ReportReason.Unsafe, ReportReason.Complaint, ReportReason.WrongOnMap, ReportReason.Other];

    public static string Label(ExclusionReason reason) => reason switch
    {
        ExclusionReason.NonResidential => "Non-residential",
        ExclusionReason.Complaint => "Complaint",
        ExclusionReason.WrongData => "Wrong data",
        ExclusionReason.Unsafe => "Unsafe",
        ExclusionReason.PrivateEstate => "Private estate",
        _ => "Other",
    };

    public static string Label(ReportReason reason) => reason switch
    {
        ReportReason.NotHomes => "Not homes",
        ReportReason.Unsafe => "Unsafe",
        ReportReason.Complaint => "Complaint",
        ReportReason.WrongOnMap => "Wrong on the map",
        _ => "Other",
    };

    public static string Label(ReportScope scope) => scope switch
    {
        ReportScope.Area => "This area",
        ReportScope.Street => "This street",
        _ => "A letterbox here",
    };

    public static string Label(ReportStatus status) => status switch
    {
        ReportStatus.SentForReview => "Sent for review",
        ReportStatus.MadeZone => "Reviewed: area excluded",
        ReportStatus.MadeAddress => "Reviewed: letterbox excluded",
        _ => "Reviewed: no change",
    };

    public static ExclusionReason ToExclusion(ReportReason reason) => reason switch
    {
        ReportReason.NotHomes => ExclusionReason.NonResidential,
        ReportReason.Unsafe => ExclusionReason.Unsafe,
        ReportReason.Complaint => ExclusionReason.Complaint,
        ReportReason.WrongOnMap => ExclusionReason.WrongData,
        _ => ExclusionReason.Other,
    };
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(List<ExclusionZone>))]
[JsonSerializable(typeof(List<ExcludedAddress>))]
[JsonSerializable(typeof(List<WalkerReport>))]
public sealed partial class ExclusionsJson : JsonSerializerContext;
