namespace GSBC.ChurchApp.Platform.Data;

public sealed class FeedHorizon
{
    public required string Feed { get; set; }
    public long Cursor { get; set; }
}
