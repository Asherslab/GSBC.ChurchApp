using System.Diagnostics.CodeAnalysis;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class MediaNewFolder([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type component)
{
    public const string ParentIdParameter = "ParentId";
    public const string ParentNameParameter = "ParentName";
    public const string OnDoneParameter = "OnDone";
    public const string OnCancelParameter = "OnCancel";

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type Component { get; } = component;
}
