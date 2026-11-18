using ProtoBuf;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

[ProtoContract]
public sealed class MapGrid
{
    [ProtoMember(1)] public double CentreLatitude { get; set; }
    [ProtoMember(2)] public double CentreLongitude { get; set; }
    [ProtoMember(3)] public double CellLatitude { get; set; }
    [ProtoMember(4)] public double CellLongitude { get; set; }
    [ProtoMember(5)] public int OriginRow { get; set; }
    [ProtoMember(6)] public int OriginColumn { get; set; }

    public MapBounds Core(int row, int column)
    {
        double latitude = CentreLatitude - (row - OriginRow) * CellLatitude;
        double longitude = CentreLongitude + (column - OriginColumn) * CellLongitude;
        return new MapBounds
        {
            South = latitude - CellLatitude / 2,
            West = longitude - CellLongitude / 2,
            North = latitude + CellLatitude / 2,
            East = longitude + CellLongitude / 2,
        };
    }

    public CellRef CellAt(double latitude, double longitude) => new()
    {
        Row = OriginRow + (int)Math.Round((CentreLatitude - latitude) / CellLatitude, MidpointRounding.AwayFromZero),
        Column = OriginColumn + (int)Math.Round((longitude - CentreLongitude) / CellLongitude, MidpointRounding.AwayFromZero),
    };

    public static bool Touching(CellRef one, CellRef other) =>
        Math.Abs(one.Row - other.Row) <= 1 && Math.Abs(one.Column - other.Column) <= 1 && !one.Equals(other);
}

[ProtoContract]
public sealed class CellRef : IEquatable<CellRef>
{
    [ProtoMember(1)] public int Row { get; init; }
    [ProtoMember(2)] public int Column { get; init; }

    public bool Equals(CellRef? other) => other is not null && other.Row == Row && other.Column == Column;

    public override bool Equals(object? obj) => Equals(obj as CellRef);

    public override int GetHashCode() => HashCode.Combine(Row, Column);
}
