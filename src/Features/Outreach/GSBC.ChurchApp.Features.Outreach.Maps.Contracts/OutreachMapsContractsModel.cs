using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Outreach.Maps.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(OutreachRegion))]
[ProtoSerializable(typeof(MapArea))]
[ProtoSerializable(typeof(MapAreaList))]
[ProtoSerializable(typeof(MapAreaListRequest))]
[ProtoSerializable(typeof(AddMapArea))]
[ProtoSerializable(typeof(MapAreaRef))]
[ProtoSerializable(typeof(RenameMapArea))]
public partial class OutreachMapsContractsModel : TypeModel;
