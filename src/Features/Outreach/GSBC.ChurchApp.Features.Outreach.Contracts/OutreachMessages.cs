using GSBC.ChurchApp.Platform.Contracts.Media;
using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf;

namespace GSBC.ChurchApp.Features.Outreach.Contracts;

[ProtoContract]
public sealed class Done;

public enum StreetSide
{
    Left = 0,
    Right = 1,
}

public enum WalkMode
{
    Letterbox = 0,
    Doorknock = 1,
}

public enum CoverageSource
{
    Casual = 0,
    DayRoute = 1,
    Area = 2,
}

public enum DoorOutcomeKind
{
    NoAnswer = 0,
    Spoke = 1,
    FollowUp = 2,
    Hostile = 3,
}

public enum ReportReason
{
    NotAHome = 0,
    Unsafe = 1,
    Complaint = 2,
    WrongOnTheMap = 3,
    Other = 4,
}

public enum ReportState
{
    New = 0,
    Excluded = 1,
    Fixed = 2,
    Dismissed = 3,
}

public enum ExclusionReason
{
    NotHomes = 0,
    Complaint = 1,
    WrongData = 2,
    Unsafe = 3,
    PrivateEstate = 4,
    Other = 5,
}

public enum TotalsBy
{
    Week = 0,
    Month = 1,
    Day = 2,
    Campaign = 3,
    OutreachDay = 4,
}

[ProtoContract]
public sealed class GeoPoint
{
    [ProtoMember(1)] public double Lat { get; set; }
    [ProtoMember(2)] public double Lng { get; set; }
}

[ProtoContract]
public sealed class Stretch
{
    [ProtoMember(1)] public string Region { get; set; } = "";
    [ProtoMember(2)] public long RegionVersion { get; set; }
    [ProtoMember(3)] public string Street { get; set; } = "";
    [ProtoMember(4)] public StreetSide Side { get; set; }
    [ProtoMember(5)] public int From { get; set; }
    [ProtoMember(6)] public int To { get; set; }
    [ProtoMember(7, IsRequired = true)] public GeoPoint Middle { get; set; } = new();
    [ProtoMember(8)] public int Metres { get; set; }
    [ProtoMember(9)] public int Letterboxes { get; set; }
}

[ProtoContract]
public sealed class AddressPoint
{
    [ProtoMember(1)] public string Region { get; set; } = "";
    [ProtoMember(2)] public string Street { get; set; } = "";
    [ProtoMember(3)] public StreetSide Side { get; set; }
    [ProtoMember(4)] public int Along { get; set; }
    [ProtoMember(5, IsRequired = true)] public GeoPoint Point { get; set; } = new();
    [ProtoMember(6)] public string Label { get; set; } = "";
    [ProtoMember(7)] public uint AddressKey { get; set; }
}

[ProtoContract]
public sealed class OutreachSettings
{
    [ProtoMember(1)] public int RestDays { get; set; }
    [ProtoMember(2)] public double WalkingKmh { get; set; }
    [ProtoMember(3)] public int HouseSeconds { get; set; }
    [ProtoMember(4)] public int UnitSeconds { get; set; }
    [ProtoMember(5)] public int DoorSeconds { get; set; }
    [ProtoMember(6)] public int UnitDoorSeconds { get; set; }
    [ProtoMember(7)] public int BundleSize { get; set; }
    [ProtoMember(8)] public double SpareShare { get; set; }
    [ProtoMember(9)] public int MaxConnectorLegMetres { get; set; }
    [ProtoMember(10)] public int MaxConnectorTotalMetres { get; set; }
    [ProtoMember(11)] public int SplitJoinMetres { get; set; }
    [ProtoMember(12)] public int MaxHopMetres { get; set; }
    [ProtoMember(13)] public double ShortOf { get; set; }
    [ProtoMember(14)] public double LongOf { get; set; }
    [ProtoMember(17)] public int NoJunkMailKeptDays { get; set; }
    [ProtoMember(18)] public int FollowUpsKeptDays { get; set; }
    [ProtoMember(19)] public double FastestKmh { get; set; }
    [ProtoMember(20)] public int LongestWalkKm { get; set; }
    [ProtoMember(21)] public bool DoorknockInPairs { get; set; }
    [ProtoMember(22)] public List<string> MinistryIds { get; set; } = [];
    [ProtoMember(23)] public DateTime ChangedAt { get; set; }
    [ProtoMember(24)] public int MaxApproachMetres { get; set; }
}

[ProtoContract]
public sealed class SettingsSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public OutreachSettings Settings { get; set; } = new();
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class MinistryRef : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class Campaign
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public string HandedOut { get; set; } = "";
    [ProtoMember(4)] public string StartsOn { get; set; } = "";
    [ProtoMember(5)] public string EndsOn { get; set; } = "";
    [ProtoMember(6)] public List<GeoPoint> Area { get; set; } = [];
    [ProtoMember(7)] public List<string> Regions { get; set; } = [];
    [ProtoMember(8)] public bool Leaderboard { get; set; }
    [ProtoMember(9)] public bool General { get; set; }
}

[ProtoContract]
public sealed class CampaignSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public Campaign Campaign { get; set; } = new();
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class ThingRef : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string Id { get; set; } = "";
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class CampaignDeletion
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public int CoveredStretches { get; set; }
    [ProtoMember(3)] public int PastDays { get; set; }
    [ProtoMember(4)] public int UpcomingDays { get; set; }
    [ProtoMember(5)] public int Walks { get; set; }
    [ProtoMember(6)] public string WhyNot { get; set; } = "";
}

[ProtoContract]
public sealed class CoveredStretch
{
    [ProtoMember(1)] public StreetSide Side { get; set; }
    [ProtoMember(2)] public int From { get; set; }
    [ProtoMember(3)] public int To { get; set; }
    [ProtoMember(4)] public DateTime At { get; set; }
    [ProtoMember(5)] public CoverageSource Source { get; set; }
}

[ProtoContract]
public sealed class StreetCoverage
{
    [ProtoMember(1)] public string Campaign { get; set; } = "";
    [ProtoMember(2)] public string Region { get; set; } = "";
    [ProtoMember(3)] public string Street { get; set; } = "";
    [ProtoMember(4)] public List<CoveredStretch> Stretches { get; set; } = [];
}

[ProtoContract]
public sealed class WalkPiece
{
    [ProtoMember(1)] public string WalkId { get; set; } = "";
    [ProtoMember(2)] public int Seq { get; set; }
    [ProtoMember(3)] public List<string> Campaigns { get; set; } = [];
    [ProtoMember(4)] public WalkMode Mode { get; set; }
    [ProtoMember(5)] public string DayId { get; set; } = "";
    [ProtoMember(6)] public string RouteId { get; set; } = "";
    [ProtoMember(7)] public DateTime StartedAt { get; set; }
    [ProtoMember(8)] public DateTime At { get; set; }
    [ProtoMember(9)] public int Metres { get; set; }
    [ProtoMember(10)] public List<Stretch> Covered { get; set; } = [];
    [ProtoMember(11)] public List<Stretch> Skipped { get; set; } = [];
    [ProtoMember(12)] public List<Stretch> Uncovered { get; set; } = [];
    [ProtoMember(13)] public int Doors { get; set; }
    [ProtoMember(14)] public int Conversations { get; set; }
    [ProtoMember(15)] public bool Ended { get; set; }
    [ProtoMember(16)] public bool WithPartner { get; set; }
}

[ProtoContract]
public sealed class NoJunkMail
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public AddressPoint Address { get; set; } = new();
}

[ProtoContract]
public sealed class MarkRef
{
    [ProtoMember(1)] public string Id { get; set; } = "";
}

[ProtoContract]
public sealed class Skip
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public AddressPoint Address { get; set; } = new();
    [ProtoMember(3)] public bool Letterbox { get; set; }
    [ProtoMember(4)] public bool Door { get; set; }
}

[ProtoContract]
public sealed class DoorOutcome
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string WalkId { get; set; } = "";
    [ProtoMember(3)] public string Campaign { get; set; } = "";
    [ProtoMember(4, IsRequired = true)] public AddressPoint Address { get; set; } = new();
    [ProtoMember(5)] public DoorOutcomeKind Outcome { get; set; }
    [ProtoMember(6)] public bool Consent { get; set; }
    [ProtoMember(7)] public string FirstName { get; set; } = "";
    [ProtoMember(8)] public string Contact { get; set; } = "";
    [ProtoMember(9)] public string Note { get; set; } = "";
    [ProtoMember(10)] public DateTime At { get; set; }
}

[ProtoContract]
public sealed class ReportSend
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public ReportReason Reason { get; set; }
    [ProtoMember(3, IsRequired = true)] public AddressPoint Place { get; set; } = new();
    [ProtoMember(4)] public string Note { get; set; } = "";
    [ProtoMember(5)] public MediaRef? Photo { get; set; }
}

[ProtoContract]
public sealed class ClaimSend
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string RouteId { get; set; } = "";
    [ProtoMember(3)] public string FirstName { get; set; } = "";
}

[ProtoContract]
public sealed class Claim
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string RouteId { get; set; } = "";
    [ProtoMember(3)] public string DayId { get; set; } = "";
    [ProtoMember(4)] public string FirstName { get; set; } = "";
    [ProtoMember(5)] public DateTime At { get; set; }
}

[ProtoContract]
public sealed class CountingSave
{
    [ProtoMember(1)] public bool ByName { get; set; }
}

[ProtoContract]
public sealed class OutreachDay
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Campaign { get; set; } = "";
    [ProtoMember(3)] public DateTime StartsAt { get; set; }
    [ProtoMember(4)] public DateTime EndsAt { get; set; }
    [ProtoMember(5, IsRequired = true)] public GeoPoint MeetingPoint { get; set; } = new();
    [ProtoMember(6)] public string MeetingPlace { get; set; } = "";
    [ProtoMember(7)] public string Note { get; set; } = "";
    [ProtoMember(8)] public WalkMode Mode { get; set; }
    [ProtoMember(9)] public bool Published { get; set; }
}

[ProtoContract]
public sealed class DayRoute
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string DayId { get; set; } = "";
    [ProtoMember(3)] public int Number { get; set; }
    [ProtoMember(4)] public string Name { get; set; } = "";
    [ProtoMember(5)] public string Code { get; set; } = "";
    [ProtoMember(6)] public List<Stretch> Stretches { get; set; } = [];
    [ProtoMember(7, IsRequired = true)] public GeoPoint Start { get; set; } = new();
    [ProtoMember(8)] public int Letterboxes { get; set; }
    [ProtoMember(9)] public int Metres { get; set; }
    [ProtoMember(10)] public int Minutes { get; set; }
    [ProtoMember(11)] public int Colour { get; set; }
}

[ProtoContract]
public sealed class DaySave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public OutreachDay Day { get; set; } = new();
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class RouteSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public DayRoute Route { get; set; } = new();
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class SavedArea
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public List<GeoPoint> Area { get; set; } = [];
    [ProtoMember(4)] public string Note { get; set; } = "";
}

[ProtoContract]
public sealed class AreaSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public SavedArea Area { get; set; } = new();
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class ExclusionZone
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
    [ProtoMember(3)] public List<GeoPoint> Area { get; set; } = [];
    [ProtoMember(4)] public ExclusionReason Reason { get; set; }
    [ProtoMember(5)] public string Note { get; set; } = "";
}

[ProtoContract]
public sealed class ExclusionAddress
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public AddressPoint Address { get; set; } = new();
    [ProtoMember(3)] public ExclusionReason Reason { get; set; }
    [ProtoMember(4)] public string Note { get; set; } = "";
    [ProtoMember(5)] public string Caller { get; set; } = "";
    [ProtoMember(6)] public bool ShowMarker { get; set; }
}

[ProtoContract]
public sealed class Exclusion
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public List<GeoPoint> Area { get; set; } = [];
    [ProtoMember(3)] public AddressPoint? Address { get; set; }
    [ProtoMember(4)] public bool ShowMarker { get; set; }
}

[ProtoContract]
public sealed class ZoneSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public ExclusionZone Zone { get; set; } = new();
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class AddressSave : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public ExclusionAddress Address { get; set; } = new();
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class Exclusions
{
    [ProtoMember(1)] public List<ExclusionZone> Zones { get; set; } = [];
    [ProtoMember(2)] public List<ExclusionAddress> Addresses { get; set; } = [];
}

[ProtoContract]
public sealed class ReportLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public ReportReason Reason { get; set; }
    [ProtoMember(3, IsRequired = true)] public AddressPoint Place { get; set; } = new();
    [ProtoMember(4)] public string Note { get; set; } = "";
    [ProtoMember(5)] public MediaRef? Photo { get; set; }
    [ProtoMember(6)] public DateTime At { get; set; }
    [ProtoMember(7)] public ReportState State { get; set; }
    [ProtoMember(8)] public string From { get; set; } = "";
    [ProtoMember(9)] public DateTime? ResolvedAt { get; set; }
}

[ProtoContract]
public sealed class Reports
{
    [ProtoMember(1)] public List<ReportLine> Lines { get; set; } = [];
}

[ProtoContract]
public sealed class ReportResolve : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string Id { get; set; } = "";
    [ProtoMember(3)] public ReportState Outcome { get; set; }
    [ProtoMember(4)] public ExclusionReason ExcludeAs { get; set; }
    [ProtoMember(5)] public bool ShowMarker { get; set; }
    [ProtoMember(6)] public List<GeoPoint> Area { get; set; } = [];
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class FollowUp
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Campaign { get; set; } = "";
    [ProtoMember(3, IsRequired = true)] public AddressPoint Address { get; set; } = new();
    [ProtoMember(4)] public string FirstName { get; set; } = "";
    [ProtoMember(5)] public string Contact { get; set; } = "";
    [ProtoMember(6)] public string Note { get; set; } = "";
    [ProtoMember(7)] public DateTime At { get; set; }
    [ProtoMember(8)] public DateTime? DoneAt { get; set; }
    [ProtoMember(9)] public string WalkedBy { get; set; } = "";
}

[ProtoContract]
public sealed class FollowUps
{
    [ProtoMember(1)] public List<FollowUp> Lines { get; set; } = [];
}

[ProtoContract]
public sealed class FollowUpDone : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string Id { get; set; } = "";
    [ProtoMember(3)] public bool Done { get; set; }
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class SkipLine
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2, IsRequired = true)] public AddressPoint Address { get; set; } = new();
    [ProtoMember(3)] public bool Hostile { get; set; }
    [ProtoMember(4)] public DateTime At { get; set; }
    [ProtoMember(5)] public DateTime Until { get; set; }
}

[ProtoContract]
public sealed class SkipLines
{
    [ProtoMember(1)] public List<SkipLine> Lines { get; set; } = [];
}

[ProtoContract]
public sealed class ExpireCoverage : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string Campaign { get; set; } = "";
    [ProtoMember(3)] public List<GeoPoint> Area { get; set; } = [];
    [ProtoMember(4)] public List<Stretch> Stretches { get; set; } = [];
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class Expired
{
    [ProtoMember(1)] public int Stretches { get; set; }
    [ProtoMember(2)] public DateTime At { get; set; }
}

[ProtoContract]
public sealed class ExpiryUndo : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string Campaign { get; set; } = "";
    [ProtoMember(3)] public DateTime At { get; set; }
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class CoverageQuery : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string Campaign { get; set; } = "";
    [ProtoMember(3)] public string Region { get; set; } = "";
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class CoverageMap
{
    [ProtoMember(1)] public List<StreetCoverage> Streets { get; set; } = [];
}

[ProtoContract]
public sealed class TotalsQuery : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public DateTime From { get; set; }
    [ProtoMember(3)] public DateTime To { get; set; }
    [ProtoMember(4)] public TotalsBy By { get; set; }
    [ProtoMember(5)] public string Campaign { get; set; } = "";
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class TotalsRow
{
    [ProtoMember(1)] public string Label { get; set; } = "";
    [ProtoMember(2)] public DateTime Starts { get; set; }
    [ProtoMember(3)] public int Walks { get; set; }
    [ProtoMember(4)] public int Metres { get; set; }
    [ProtoMember(5)] public int Letterboxes { get; set; }
    [ProtoMember(6)] public int Doors { get; set; }
    [ProtoMember(7)] public int Conversations { get; set; }
    [ProtoMember(8)] public int Walkers { get; set; }
}

[ProtoContract]
public sealed class WalkerTotal
{
    [ProtoMember(1)] public string Name { get; set; } = "";
    [ProtoMember(2)] public int Walks { get; set; }
    [ProtoMember(3)] public int Metres { get; set; }
    [ProtoMember(4)] public int Letterboxes { get; set; }
    [ProtoMember(5)] public int Doors { get; set; }
}

[ProtoContract]
public sealed class Totals
{
    [ProtoMember(1)] public List<TotalsRow> Rows { get; set; } = [];
    [ProtoMember(2)] public List<WalkerTotal> Walkers { get; set; } = [];
    [ProtoMember(3, IsRequired = true)] public WalkerTotal Anonymous { get; set; } = new();
}

[ProtoContract]
public sealed class Leaderboard
{
    [ProtoMember(1)] public string Campaign { get; set; } = "";
    [ProtoMember(2)] public List<WalkerTotal> Walkers { get; set; } = [];
    [ProtoMember(3, IsRequired = true)] public WalkerTotal Everyone { get; set; } = new();
}

[ProtoContract]
public sealed class YourWalk
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public DateTime StartedAt { get; set; }
    [ProtoMember(3)] public DateTime LastAt { get; set; }
    [ProtoMember(4)] public bool Ended { get; set; }
    [ProtoMember(5)] public int Metres { get; set; }
    [ProtoMember(6)] public int Letterboxes { get; set; }
    [ProtoMember(7)] public int Doors { get; set; }
    [ProtoMember(8)] public int Conversations { get; set; }
    [ProtoMember(9)] public List<string> Campaigns { get; set; } = [];
    [ProtoMember(10)] public WalkMode Mode { get; set; }
    [ProtoMember(11)] public bool Counted { get; set; }
}

[ProtoContract]
public sealed class YourCounting
{
    [ProtoMember(1)] public bool ByName { get; set; }
}

[ProtoContract]
public sealed class BoardQuery : IScopedRequest
{
    [ProtoMember(1)] public string MinistryId { get; set; } = "";
    [ProtoMember(2)] public string DayId { get; set; } = "";
    public string ScopeId => MinistryId;
}

[ProtoContract]
public sealed class RouteProgress
{
    [ProtoMember(1)] public string RouteId { get; set; } = "";
    [ProtoMember(2)] public List<string> ClaimedBy { get; set; } = [];
    [ProtoMember(3)] public int Letterboxes { get; set; }
    [ProtoMember(4)] public int LetterboxesDone { get; set; }
    [ProtoMember(5)] public List<Stretch> Done { get; set; } = [];
}

[ProtoContract]
public sealed class DayBoard
{
    [ProtoMember(1, IsRequired = true)] public OutreachDay Day { get; set; } = new();
    [ProtoMember(2)] public List<DayRoute> Routes { get; set; } = [];
    [ProtoMember(3)] public List<RouteProgress> Progress { get; set; } = [];
    [ProtoMember(4)] public int Walking { get; set; }
}

[ProtoContract]
public sealed class OutreachDesk
{
    [ProtoMember(1, IsRequired = true)] public OutreachSettings Settings { get; set; } = new();
    [ProtoMember(2)] public List<Campaign> Campaigns { get; set; } = [];
    [ProtoMember(3)] public List<OutreachDay> Days { get; set; } = [];
    [ProtoMember(4)] public List<SavedArea> Areas { get; set; } = [];
    [ProtoMember(5)] public List<OutreachMinistry> Ministries { get; set; } = [];
    [ProtoMember(6)] public List<CampaignTotals> Totals { get; set; } = [];
    [ProtoMember(7)] public int OpenFollowUps { get; set; }
    [ProtoMember(8)] public int NewReports { get; set; }
    [ProtoMember(9)] public List<DayFacts> DayFacts { get; set; } = [];
}

[ProtoContract]
public sealed class CampaignTotals
{
    [ProtoMember(1)] public string Campaign { get; set; } = "";
    [ProtoMember(2)] public int LetterboxesThisMonth { get; set; }
    [ProtoMember(3)] public int Letterboxes { get; set; }
    [ProtoMember(4)] public int Metres { get; set; }
    [ProtoMember(5)] public int Walks { get; set; }
    [ProtoMember(6)] public int Days { get; set; }
}

[ProtoContract]
public sealed class RunningMinistries
{
    [ProtoMember(1)] public List<OutreachMinistry> Ministries { get; set; } = [];
}

[ProtoContract]
public sealed class OutreachMinistry
{
    [ProtoMember(1)] public string Id { get; set; } = "";
    [ProtoMember(2)] public string Name { get; set; } = "";
}

[ProtoContract]
public sealed class DayRoutes
{
    [ProtoMember(1)] public List<DayRoute> Routes { get; set; } = [];
}
