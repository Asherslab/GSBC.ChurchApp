namespace GSBC.ChurchApp.Features.Outreach.Routing;

public enum WalkKind
{
    Letterbox,
    Doorknock,
}

public sealed record RouteSettings
{
    public static RouteSettings Defaults { get; } = new();

    public double WalkingKmh { get; init; } = 4;

    public double HouseSeconds { get; init; } = 8;

    public double UnitSeconds { get; init; } = 3;

    public double DoorSeconds { get; init; } = 90;

    public double UnitDoorSeconds { get; init; } = 60;

    public int BundleSize { get; init; } = 50;

    public double SpareShare { get; init; } = 0.1;

    public int RestDays { get; init; } = 91;

    public double MaxConnectorLegMetres { get; init; } = 150;

    public double MaxConnectorTotalMetres { get; init; } = 250;

    public double SplitJoinMetres { get; init; } = 125;

    public double MaxHopMetres { get; init; } = 600;

    public double ShortOf { get; init; } = 0.9;

    public double LongOf { get; init; } = 1.25;

    public double WalkingMetresPerSecond => WalkingKmh * 1000 / 3600;

    public double SecondsPer(LetterboxKind kind, WalkKind walk) => (kind, walk) switch
    {
        (LetterboxKind.Skip, _) => 0,
        (LetterboxKind.Unit, WalkKind.Letterbox) => UnitSeconds,
        (_, WalkKind.Letterbox) => HouseSeconds,
        (LetterboxKind.Unit, WalkKind.Doorknock) => UnitDoorSeconds,
        _ => DoorSeconds,
    };
}
