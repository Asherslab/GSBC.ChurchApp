using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Media.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(BeginUploadRequest))]
[ProtoSerializable(typeof(UploadTicket))]
[ProtoSerializable(typeof(CompleteUploadRequest))]
[ProtoSerializable(typeof(UploadedMedia))]
[ProtoSerializable(typeof(FindByHashRequest))]
[ProtoSerializable(typeof(MediaMatches))]
[ProtoSerializable(typeof(MediaRef))]
[ProtoSerializable(typeof(LinkRequest))]
[ProtoSerializable(typeof(MediaLink))]
[ProtoSerializable(typeof(MediaQuery))]
[ProtoSerializable(typeof(MediaList))]
[ProtoSerializable(typeof(MediaCounts))]
[ProtoSerializable(typeof(FolderQuery))]
[ProtoSerializable(typeof(FolderTree))]
[ProtoSerializable(typeof(FolderInfo))]
[ProtoSerializable(typeof(CreateFolderRequest))]
[ProtoSerializable(typeof(RenameFolderRequest))]
[ProtoSerializable(typeof(MoveRequest))]
[ProtoSerializable(typeof(DeleteFolderRequest))]
[ProtoSerializable(typeof(RemoveOriginalRequest))]
[ProtoSerializable(typeof(MediaItemInfo))]
[ProtoSerializable(typeof(DeleteMediaRequest))]
[ProtoSerializable(typeof(Done))]
[ProtoSerializable(typeof(FontSearch))]
[ProtoSerializable(typeof(FontFamilies))]
[ProtoSerializable(typeof(FontFamilyChoice))]
[ProtoSerializable(typeof(ImportFontRequest))]
[ProtoSerializable(typeof(CustomFont))]
[ProtoSerializable(typeof(FontFile))]
public partial class MediaContractsModel : TypeModel;
