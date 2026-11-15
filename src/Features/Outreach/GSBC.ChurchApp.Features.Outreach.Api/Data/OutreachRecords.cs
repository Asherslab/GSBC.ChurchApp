using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Api.Data;

public sealed class SettingsRecord
{
    public int Id { get; set; }
    public byte[] Payload { get; set; } = [];
    public DateTime ChangedAt { get; set; }
    public Guid? ChangedBy { get; set; }
}

public sealed class CampaignRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string HandedOut { get; set; } = "";
    public DateOnly? StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public List<double> AreaLats { get; set; } = [];
    public List<double> AreaLngs { get; set; } = [];
    public List<string> Regions { get; set; } = [];
    public bool Leaderboard { get; set; }
    public bool General { get; set; }
    public CampaignStage Stage { get; set; }
    public DateOnly StageSetOn { get; set; }
    public int WrapUpDays { get; set; } = CampaignStages.DefaultWrapUpDays;
    public int BundleSize { get; set; }
    public int BundlesMade { get; set; }
    public DateTime? ToldEndingAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ChangedAt { get; set; }
}

public sealed class CoverageRecord
{
    public long Id { get; set; }
    public Guid CampaignId { get; set; }
    public string Region { get; set; } = "";
    public long RegionVersion { get; set; }
    public string Street { get; set; } = "";
    public StreetSide Side { get; set; }
    public int From { get; set; }
    public int To { get; set; }
    public double MiddleLat { get; set; }
    public double MiddleLng { get; set; }
    public int Metres { get; set; }
    public int Letterboxes { get; set; }
    public DateTime At { get; set; }
    public CoverageSource Source { get; set; }
    public Guid? WalkId { get; set; }
    public Guid? DayId { get; set; }
    public DateTime? ExpiredAt { get; set; }
}

public sealed class WalkRecord
{
    public Guid Id { get; set; }
    public Guid? InstallId { get; set; }
    public Guid? UserId { get; set; }
    public List<Guid> Campaigns { get; set; } = [];
    public WalkMode Mode { get; set; }
    public Guid? DayId { get; set; }
    public Guid? RouteId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastAt { get; set; }
    public bool Ended { get; set; }
    public int Metres { get; set; }
    public int Letterboxes { get; set; }
    public int Doors { get; set; }
    public int Conversations { get; set; }
    public bool WithPartner { get; set; }
    public string Doubtful { get; set; } = "";
}

public sealed class WalkPieceRecord
{
    public Guid WalkId { get; set; }
    public int Seq { get; set; }
    public DateTime At { get; set; }
    public int Metres { get; set; }
    public bool Counted { get; set; }
}

public enum SkipKind
{
    NoJunkMail = 0,
    Hostile = 1,
}

public sealed class SkipRecord
{
    public Guid Id { get; set; }
    public SkipKind Kind { get; set; }
    public uint AddressKey { get; set; }
    public string Region { get; set; } = "";
    public string Street { get; set; } = "";
    public StreetSide Side { get; set; }
    public int Along { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Label { get; set; } = "";
    public Guid? InstallId { get; set; }
    public Guid? UserId { get; set; }
    public DateTime At { get; set; }
    public DateTime? Until { get; set; }
}

public sealed class DoorRecord
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid? WalkId { get; set; }
    public Guid? InstallId { get; set; }
    public uint AddressKey { get; set; }
    public string Street { get; set; } = "";
    public StreetSide Side { get; set; }
    public int Along { get; set; }
    public DateTime At { get; set; }
}

public sealed class FollowUpRecord
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public uint AddressKey { get; set; }
    public string Region { get; set; } = "";
    public string Street { get; set; } = "";
    public StreetSide Side { get; set; }
    public int Along { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Label { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string Contact { get; set; } = "";
    public string Note { get; set; } = "";
    public DateTime At { get; set; }
    public DateTime? DoneAt { get; set; }
    public Guid? InstallId { get; set; }
    public Guid? UserId { get; set; }
}

public sealed class ReportRecord
{
    public Guid Id { get; set; }
    public ReportReason Reason { get; set; }
    public uint AddressKey { get; set; }
    public string Region { get; set; } = "";
    public string Street { get; set; } = "";
    public StreetSide Side { get; set; }
    public int Along { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Label { get; set; } = "";
    public string Note { get; set; } = "";
    public byte[]? Photo { get; set; }
    public ReportState State { get; set; }
    public DateTime At { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public Guid? InstallId { get; set; }
    public Guid? UserId { get; set; }
}

public sealed class ExclusionZoneRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<double> AreaLats { get; set; } = [];
    public List<double> AreaLngs { get; set; } = [];
    public ExclusionReason Reason { get; set; }
    public string Note { get; set; } = "";
    public DateTime ChangedAt { get; set; }
    public Guid? ChangedBy { get; set; }
}

public sealed class ExclusionAddressRecord
{
    public Guid Id { get; set; }
    public uint AddressKey { get; set; }
    public string Region { get; set; } = "";
    public string Street { get; set; } = "";
    public StreetSide Side { get; set; }
    public int Along { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Label { get; set; } = "";
    public ExclusionReason Reason { get; set; }
    public string Note { get; set; } = "";
    public string Caller { get; set; } = "";
    public bool ShowMarker { get; set; }
    public DateTime ChangedAt { get; set; }
    public Guid? ChangedBy { get; set; }
}

public sealed class DayRecord
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public double MeetingLat { get; set; }
    public double MeetingLng { get; set; }
    public string MeetingPlace { get; set; } = "";
    public string Note { get; set; } = "";
    public WalkMode Mode { get; set; }
    public bool Published { get; set; }
    public bool OnPhones { get; set; }
    public byte[]? Draft { get; set; }
    public DateTime? RoutesPublishedAt { get; set; }
    public DateTime ChangedAt { get; set; }
}

public sealed class RouteRecord
{
    public Guid Id { get; set; }
    public Guid DayId { get; set; }
    public int Number { get; set; }
    public string Code { get; set; } = "";
    public byte[] Payload { get; set; } = [];
    public DateTime ChangedAt { get; set; }
}

public sealed class ClaimRecord
{
    public Guid Id { get; set; }
    public Guid RouteId { get; set; }
    public Guid DayId { get; set; }
    public Guid? InstallId { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = "";
    public DateTime At { get; set; }
}

public sealed class AreaRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public List<double> AreaLats { get; set; } = [];
    public List<double> AreaLngs { get; set; } = [];
    public string Note { get; set; } = "";
    public DateTime ChangedAt { get; set; }
}

public sealed class WalkerRecord
{
    public Guid UserId { get; set; }
    public bool ByName { get; set; } = true;
    public DateTime ChangedAt { get; set; }
}
