namespace GSBC.ChurchApp.Features.Outreach.Contracts;

public static class OutreachDefaults
{
    public static OutreachSettings Settings() => new()
    {
        RestDays = 91,
        WalkingKmh = 4,
        HouseSeconds = 8,
        UnitSeconds = 3,
        DoorSeconds = 90,
        UnitDoorSeconds = 60,
        BundleSize = 50,
        SpareShare = 0.1,
        MaxConnectorLegMetres = 150,
        MaxConnectorTotalMetres = 250,
        SplitJoinMetres = 125,
        MaxHopMetres = 600,
        MaxApproachMetres = 500,
        ShortOf = 0.9,
        LongOf = 1.25,
        NoJunkMailKeptDays = 730,
        FollowUpsKeptDays = 90,
        FastestKmh = 9,
        LongestWalkKm = 30,
        DoorknockInPairs = true,
    };
}
