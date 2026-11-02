using System.Diagnostics.CodeAnalysis;

namespace GSBC.ChurchApp.Platform.Client.Media;

public sealed class MediaPicker([DynamicallyAccessedMembers(MediaPicker.ComponentMembers)] Type component)
{
    public const string ImagesOnlyParameter = "ImagesOnly";
    public const string OnPickParameter = "OnPick";

    private const DynamicallyAccessedMemberTypes ComponentMembers =
        DynamicallyAccessedMemberTypes.All;

    [DynamicallyAccessedMembers(ComponentMembers)]
    public Type Component { get; } = component;
}
