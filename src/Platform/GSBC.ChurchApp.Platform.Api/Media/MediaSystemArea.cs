using GSBC.ChurchApp.Platform.Permissions;

namespace GSBC.ChurchApp.Platform.Api.Media;

public sealed record MediaSystemArea(
    string Key, string Name, string Caption, Permission Audience, bool StaffDeleteUnused = false, Permission? Uploaders = null,
    IReadOnlyList<AreaFileType>? Files = null);

public sealed record AreaFileType(string ContentType, long LargestBytes);
