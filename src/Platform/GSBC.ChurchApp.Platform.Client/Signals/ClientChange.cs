namespace GSBC.ChurchApp.Platform.Client.Signals;

[Flags]
public enum ChangeKind
{
    None = 0,
    Feeds = 1,
    Reconnected = 2,
    Disconnected = 4,
    Session = 8,
    Access = 16,
    Outbox = 32,
}

public sealed record ClientChange(ChangeKind Kind, IReadOnlySet<string> Feeds)
{
    public bool Concerns(ChangeKind kinds, IReadOnlyCollection<string>? feeds)
    {
        if ((Kind & kinds & ~ChangeKind.Feeds) != ChangeKind.None)
            return true;
        return Kind.HasFlag(ChangeKind.Feeds) && kinds.HasFlag(ChangeKind.Feeds) && (feeds is null || feeds.Any(Feeds.Contains));
    }
}
