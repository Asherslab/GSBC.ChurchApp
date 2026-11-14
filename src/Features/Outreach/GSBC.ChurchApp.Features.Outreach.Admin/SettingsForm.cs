using System.Globalization;
using GSBC.ChurchApp.Features.Outreach.Contracts;

namespace GSBC.ChurchApp.Features.Outreach.Admin;

public enum SettingsGroup
{
    Keeping,
    Times,
    Daylight,
    Between,
    Length,
    Counting,
}

public sealed class SettingsForm
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public string RestDays { get; set; } = "";
    public string NoJunkMailKeptDays { get; set; } = "";
    public string FollowUpsKeptDays { get; set; } = "";
    public string WalkingKmh { get; set; } = "";
    public string BundleSize { get; set; } = "";
    public string HouseSeconds { get; set; } = "";
    public string UnitSeconds { get; set; } = "";
    public string DoorSeconds { get; set; } = "";
    public string UnitDoorSeconds { get; set; } = "";
    public string DaylightStarts { get; set; } = "";
    public string DaylightEnds { get; set; } = "";
    public bool DoorknockInPairs { get; set; }
    public string MaxApproachMetres { get; set; } = "";
    public string MaxConnectorLegMetres { get; set; } = "";
    public string MaxConnectorTotalMetres { get; set; } = "";
    public string SplitJoinMetres { get; set; } = "";
    public string MaxHopMetres { get; set; } = "";
    public string ShortOfPercent { get; set; } = "";
    public string LongOfPercent { get; set; } = "";
    public string FastestKmh { get; set; } = "";
    public string LongestWalkKm { get; set; } = "";

    public static SettingsForm Of(OutreachSettings settings) => new()
    {
        RestDays = Whole(settings.RestDays),
        NoJunkMailKeptDays = Whole(settings.NoJunkMailKeptDays),
        FollowUpsKeptDays = Whole(settings.FollowUpsKeptDays),
        WalkingKmh = Decimal(settings.WalkingKmh),
        BundleSize = Whole(settings.BundleSize),
        HouseSeconds = Whole(settings.HouseSeconds),
        UnitSeconds = Whole(settings.UnitSeconds),
        DoorSeconds = Whole(settings.DoorSeconds),
        UnitDoorSeconds = Whole(settings.UnitDoorSeconds),
        DaylightStarts = Time(settings.DaylightStartsMinute),
        DaylightEnds = Time(settings.DaylightEndsMinute),
        DoorknockInPairs = settings.DoorknockInPairs,
        MaxApproachMetres = Whole(settings.MaxApproachMetres),
        MaxConnectorLegMetres = Whole(settings.MaxConnectorLegMetres),
        MaxConnectorTotalMetres = Whole(settings.MaxConnectorTotalMetres),
        SplitJoinMetres = Whole(settings.SplitJoinMetres),
        MaxHopMetres = Whole(settings.MaxHopMetres),
        ShortOfPercent = Whole((int)Math.Round(settings.ShortOf * 100)),
        LongOfPercent = Whole((int)Math.Round(settings.LongOf * 100)),
        FastestKmh = Decimal(settings.FastestKmh),
        LongestWalkKm = Whole(settings.LongestWalkKm),
    };

    public string? Problem => Settings(OutreachDefaults.Settings()) is null ? "Fill in every number, using digits." : null;

    public OutreachSettings? Settings(OutreachSettings basis)
    {
        if (!(Int(RestDays) is { } rest && Int(NoJunkMailKeptDays) is { } noJunk && Int(FollowUpsKeptDays) is { } followUps && Double(WalkingKmh) is { } pace
              && Int(BundleSize) is { } bundle && Int(HouseSeconds) is { } house && Int(UnitSeconds) is { } unit && Int(DoorSeconds) is { } door
              && Int(UnitDoorSeconds) is { } unitDoor && Minute(DaylightStarts) is { } from && Minute(DaylightEnds) is { } until
              && Int(MaxApproachMetres) is { } approach && Int(MaxConnectorLegMetres) is { } leg && Int(MaxConnectorTotalMetres) is { } total
              && Int(SplitJoinMetres) is { } join && Int(MaxHopMetres) is { } hop && Int(ShortOfPercent) is { } shortOf && Int(LongOfPercent) is { } longOf
              && Double(FastestKmh) is { } fastest && Int(LongestWalkKm) is { } longest))
            return null;
        OutreachSettings settings = OutreachPayloadCopy.Of(basis);
        (settings.RestDays, settings.NoJunkMailKeptDays, settings.FollowUpsKeptDays) = (rest, noJunk, followUps);
        (settings.WalkingKmh, settings.BundleSize, settings.HouseSeconds, settings.UnitSeconds) = (pace, bundle, house, unit);
        (settings.DoorSeconds, settings.UnitDoorSeconds, settings.DaylightStartsMinute, settings.DaylightEndsMinute) = (door, unitDoor, from, until);
        settings.DoorknockInPairs = DoorknockInPairs;
        (settings.MaxApproachMetres, settings.MaxConnectorLegMetres, settings.MaxConnectorTotalMetres) = (approach, leg, total);
        (settings.SplitJoinMetres, settings.MaxHopMetres, settings.ShortOf, settings.LongOf) = (join, hop, shortOf / 100.0, longOf / 100.0);
        (settings.FastestKmh, settings.LongestWalkKm) = (fastest, longest);
        return settings;
    }

    public static readonly IReadOnlyList<SettingsGroup> Groups = Enum.GetValues<SettingsGroup>();

    public bool Same(SettingsForm other) => Groups.All(group => Same(group, other));

    public bool Same(SettingsGroup group, SettingsForm other) => group switch
    {
        SettingsGroup.Keeping => (RestDays, NoJunkMailKeptDays, FollowUpsKeptDays) == (other.RestDays, other.NoJunkMailKeptDays, other.FollowUpsKeptDays),
        SettingsGroup.Times => (WalkingKmh, BundleSize, HouseSeconds, UnitSeconds, DoorSeconds, UnitDoorSeconds)
                               == (other.WalkingKmh, other.BundleSize, other.HouseSeconds, other.UnitSeconds, other.DoorSeconds, other.UnitDoorSeconds),
        SettingsGroup.Daylight => (DaylightStarts, DaylightEnds, DoorknockInPairs) == (other.DaylightStarts, other.DaylightEnds, other.DoorknockInPairs),
        SettingsGroup.Between => (MaxApproachMetres, MaxConnectorLegMetres, MaxConnectorTotalMetres, SplitJoinMetres, MaxHopMetres)
                                 == (other.MaxApproachMetres, other.MaxConnectorLegMetres, other.MaxConnectorTotalMetres, other.SplitJoinMetres, other.MaxHopMetres),
        SettingsGroup.Length => (ShortOfPercent, LongOfPercent) == (other.ShortOfPercent, other.LongOfPercent),
        _ => (FastestKmh, LongestWalkKm) == (other.FastestKmh, other.LongestWalkKm),
    };

    public void Take(SettingsGroup group, SettingsForm from)
    {
        switch (group)
        {
            case SettingsGroup.Keeping:
                (RestDays, NoJunkMailKeptDays, FollowUpsKeptDays) = (from.RestDays, from.NoJunkMailKeptDays, from.FollowUpsKeptDays);
                break;
            case SettingsGroup.Times:
                (WalkingKmh, BundleSize, HouseSeconds) = (from.WalkingKmh, from.BundleSize, from.HouseSeconds);
                (UnitSeconds, DoorSeconds, UnitDoorSeconds) = (from.UnitSeconds, from.DoorSeconds, from.UnitDoorSeconds);
                break;
            case SettingsGroup.Daylight:
                (DaylightStarts, DaylightEnds, DoorknockInPairs) = (from.DaylightStarts, from.DaylightEnds, from.DoorknockInPairs);
                break;
            case SettingsGroup.Between:
                (MaxApproachMetres, MaxConnectorLegMetres, MaxConnectorTotalMetres) = (from.MaxApproachMetres, from.MaxConnectorLegMetres, from.MaxConnectorTotalMetres);
                (SplitJoinMetres, MaxHopMetres) = (from.SplitJoinMetres, from.MaxHopMetres);
                break;
            case SettingsGroup.Length:
                (ShortOfPercent, LongOfPercent) = (from.ShortOfPercent, from.LongOfPercent);
                break;
            default:
                (FastestKmh, LongestWalkKm) = (from.FastestKmh, from.LongestWalkKm);
                break;
        }
    }

    private static string Whole(int value) => value.ToString(Invariant);

    private static string Decimal(double value) => value.ToString("0.##", Invariant);

    private static string Time(int minute) => $"{minute / 60:00}:{minute % 60:00}";

    private static int? Int(string text) => int.TryParse(text.Trim(), NumberStyles.Integer, Invariant, out int value) ? value : null;

    private static double? Double(string text) => double.TryParse(text.Trim(), NumberStyles.Float, Invariant, out double value) ? value : null;

    private static int? Minute(string text) => TimeOnly.TryParse(text, Invariant, out TimeOnly time) ? time.Hour * 60 + time.Minute : null;
}

internal static class OutreachPayloadCopy
{
    public static OutreachSettings Of(OutreachSettings settings)
    {
        using MemoryStream stream = new();
        OutreachContractsModel.Instance.Serialize(stream, settings);
        stream.Position = 0;
        return OutreachContractsModel.Instance.Deserialize<OutreachSettings>(stream);
    }
}
