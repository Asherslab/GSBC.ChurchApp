using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Sync;

public abstract record FeedAudience
{
    public static readonly FeedAudience Public = new PublicAudience();
    public static readonly FeedAudience SignedIn = new SignedInAudience();
    public static readonly FeedAudience OwnRecords = new OwnRecordsAudience();

    public static FeedAudience Holding(Permission permission) => new PermissionAudience(permission);

    public sealed record PublicAudience : FeedAudience;

    public sealed record SignedInAudience : FeedAudience;

    public sealed record OwnRecordsAudience : FeedAudience;

    public sealed record PermissionAudience(Permission Permission) : FeedAudience;
}
