using System.Diagnostics.CodeAnalysis;

namespace GSBC.ChurchApp.Platform.Client.Media;

public sealed class MediaPicker([DynamicallyAccessedMembers(MediaPicker.ComponentMembers)] Type component)
{
    public const string ImagesOnlyParameter = "ImagesOnly";
    public const string FontsOnlyParameter = "FontsOnly";
    public const string OnPickParameter = "OnPick";
    public const string OnCancelParameter = "OnCancel";
    public const string SizeParameter = "Size";
    public const string OfferedParameter = "Offered";
    public const string KindsParameter = "Kinds";
    public const string UploadToParameter = "UploadTo";

    private const DynamicallyAccessedMemberTypes ComponentMembers =
        DynamicallyAccessedMemberTypes.All;

    [DynamicallyAccessedMembers(ComponentMembers)]
    public Type Component { get; } = component;
}
