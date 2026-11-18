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
[ProtoSerializable(typeof(RegionList))]
[ProtoSerializable(typeof(RegionListRequest))]
[ProtoSerializable(typeof(MapOverview))]
[ProtoSerializable(typeof(MapOverviewRequest))]
[ProtoSerializable(typeof(MapCorner))]
[ProtoSerializable(typeof(AddMapArea))]
[ProtoSerializable(typeof(MapAreaRef))]
[ProtoSerializable(typeof(RenameMapArea))]
[ProtoSerializable(typeof(MapGrid))]
[ProtoSerializable(typeof(CellRef))]
[ProtoSerializable(typeof(MapCell))]
[ProtoSerializable(typeof(PlaceSearch))]
[ProtoSerializable(typeof(PlaceList))]
[ProtoSerializable(typeof(MapPlace))]
[ProtoSerializable(typeof(MapAreaPlan))]
[ProtoSerializable(typeof(MapAreaPreview))]
[ProtoSerializable(typeof(CellEstimate))]
[ProtoSerializable(typeof(RegionChange))]
[ProtoSerializable(typeof(RegionChangeCheck))]
[ProtoSerializable(typeof(RegionUse))]
public partial class OutreachMapsContractsModel : TypeModel;
