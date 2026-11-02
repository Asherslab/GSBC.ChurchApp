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
[ProtoSerializable(typeof(MediaRef))]
[ProtoSerializable(typeof(LinkRequest))]
[ProtoSerializable(typeof(MediaLink))]
[ProtoSerializable(typeof(MediaQuery))]
[ProtoSerializable(typeof(MediaList))]
public partial class MediaContractsModel : TypeModel;
