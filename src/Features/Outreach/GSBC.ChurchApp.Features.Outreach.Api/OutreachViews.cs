using GSBC.ChurchApp.Features.Outreach.Api.Data;
using GSBC.ChurchApp.Features.Outreach.Contracts;
using GSBC.ChurchApp.Platform.Common;
using GSBC.ChurchApp.Platform.Contracts.Media;

namespace GSBC.ChurchApp.Features.Outreach.Api;

internal static class OutreachViews
{
    public static Campaign Campaign(CampaignRecord campaign) => new()
    {
        Id = campaign.Id.ToString(),
        Name = campaign.Name,
        HandedOut = campaign.HandedOut,
        StartsOn = campaign.StartsOn is { } starts ? OutreachRules.Format(starts) : "",
        EndsOn = campaign.EndsOn is { } ends ? OutreachRules.Format(ends) : "",
        Area = Polygon(campaign.AreaLats, campaign.AreaLngs),
        Regions = [.. campaign.Regions],
        Leaderboard = campaign.Leaderboard,
        General = campaign.General,
        Stage = campaign.General ? CampaignStage.Ongoing : campaign.Stage,
        StageSetOn = OutreachRules.Format(campaign.StageSetOn),
        WrapUpDays = campaign.WrapUpDays,
        BundleSize = campaign.BundleSize,
        BundlesMade = campaign.BundlesMade,
        ToldEndingOn = campaign.ToldEndingAt is { } told ? OutreachRules.Format(ChurchTime.DayOf(told)) : "",
    };

    public static List<GeoPoint> Polygon(IReadOnlyList<double> lats, IReadOnlyList<double> lngs) =>
        lats.Zip(lngs, (lat, lng) => new GeoPoint { Lat = lat, Lng = lng }).ToList();

    public static AddressPoint Address(uint key, string region, string street, StreetSide side, int along, double lat, double lng, string label) => new()
    {
        AddressKey = key, Region = region, Street = street, Side = side, Along = along, Point = new GeoPoint { Lat = lat, Lng = lng }, Label = label,
    };

    public static OutreachDay Day(DayRecord day) => new()
    {
        Id = day.Id.ToString(),
        Campaign = day.CampaignId.ToString(),
        StartsAt = day.StartsAt,
        EndsAt = day.EndsAt,
        MeetingPoint = new GeoPoint { Lat = day.MeetingLat, Lng = day.MeetingLng },
        MeetingPlace = day.MeetingPlace,
        Note = day.Note,
        Mode = day.Mode,
        Published = day.Published,
    };

    public static Skip Skip(SkipRecord skip) => new()
    {
        Id = skip.Id.ToString(),
        Address = Address(skip.AddressKey, skip.Region, skip.Street, skip.Side, skip.Along, skip.Lat, skip.Lng, ""),
        Letterbox = true,
        Door = skip.Kind == SkipKind.Hostile,
    };

    public static SkipLine SkipLine(SkipRecord skip) => new()
    {
        Id = skip.Id.ToString(),
        Address = Address(skip.AddressKey, skip.Region, skip.Street, skip.Side, skip.Along, skip.Lat, skip.Lng, skip.Label),
        Hostile = skip.Kind == SkipKind.Hostile,
        At = skip.At,
        Until = skip.Until ?? DateTime.MaxValue,
    };

    public static YourWalk Yours(WalkRecord walk, bool counted) => new()
    {
        Id = walk.Id.ToString(),
        StartedAt = walk.StartedAt,
        LastAt = walk.LastAt,
        Ended = walk.Ended,
        Metres = walk.Metres,
        Letterboxes = walk.Letterboxes,
        Doors = walk.Doors,
        Conversations = walk.Conversations,
        Campaigns = walk.Campaigns.Select(campaign => campaign.ToString()).ToList(),
        Mode = walk.Mode,
        Counted = counted,
    };

    public static FollowUp FollowUp(FollowUpRecord followUp, string walkedBy) => new()
    {
        Id = followUp.Id.ToString(),
        Campaign = followUp.CampaignId.ToString(),
        Address = Address(followUp.AddressKey, followUp.Region, followUp.Street, followUp.Side, followUp.Along, followUp.Lat, followUp.Lng, followUp.Label),
        FirstName = followUp.FirstName,
        Contact = followUp.Contact,
        Note = followUp.Note,
        At = followUp.At,
        DoneAt = followUp.DoneAt,
        WalkedBy = walkedBy,
    };

    public static ReportLine Report(ReportRecord report, string from) => new()
    {
        Id = report.Id.ToString(),
        Reason = report.Reason,
        Place = Address(report.AddressKey, report.Region, report.Street, report.Side, report.Along, report.Lat, report.Lng, report.Label),
        Note = report.Note,
        Photo = report.Photo is { } photo ? OutreachPayloads.Decode<MediaRef>(photo) : null,
        At = report.At,
        State = report.State,
        From = from,
        ResolvedAt = report.ResolvedAt,
    };

    public static SavedArea Area(AreaRecord area) => new()
    {
        Id = area.Id.ToString(), Name = area.Name, Area = Polygon(area.AreaLats, area.AreaLngs), Note = area.Note,
    };

    public static Exclusion Exclusion(ExclusionZoneRecord zone) => new() { Id = zone.Id.ToString(), Area = Polygon(zone.AreaLats, zone.AreaLngs) };

    public static Exclusion Exclusion(ExclusionAddressRecord address) => new()
    {
        Id = address.Id.ToString(),
        Address = Address(address.AddressKey, address.Region, address.Street, address.Side, address.Along, address.Lat, address.Lng, address.ShowMarker ? address.Label : ""),
        ShowMarker = address.ShowMarker,
    };

    public static ExclusionZone Zone(ExclusionZoneRecord zone) => new()
    {
        Id = zone.Id.ToString(), Name = zone.Name, Area = Polygon(zone.AreaLats, zone.AreaLngs), Reason = zone.Reason, Note = zone.Note,
    };

    public static ExclusionAddress ExcludedAddress(ExclusionAddressRecord address) => new()
    {
        Id = address.Id.ToString(),
        Address = Address(address.AddressKey, address.Region, address.Street, address.Side, address.Along, address.Lat, address.Lng, address.Label),
        Reason = address.Reason,
        Note = address.Note,
        Caller = address.Caller,
        ShowMarker = address.ShowMarker,
    };
}
