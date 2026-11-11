using System.Diagnostics.CodeAnalysis;

namespace GSBC.ChurchApp.Features.Media.Client;

public sealed class MediaDirectUpload([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type component)
{
    public const string LabelParameter = "Label";
    public const string FontsOnlyParameter = "FontsOnly";
    public const string FolderIdParameter = "FolderId";
    public const string OnUploadedParameter = "OnUploaded";

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type Component { get; } = component;
}
