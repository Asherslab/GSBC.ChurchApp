using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(Placement))]
[ProtoSerializable(typeof(AreaQuery))]
[ProtoSerializable(typeof(AreaPlacements))]
[ProtoSerializable(typeof(ChangeList))]
[ProtoSerializable(typeof(RollBackRequest))]
[ProtoSerializable(typeof(Draft))]
[ProtoSerializable(typeof(DraftList))]
[ProtoSerializable(typeof(EventLayerMessage))]
[ProtoSerializable(typeof(ListDraftsRequest))]
[ProtoSerializable(typeof(DraftQuery))]
[ProtoSerializable(typeof(NewDraftRequest))]
[ProtoSerializable(typeof(RenameDraftRequest))]
[ProtoSerializable(typeof(SaveDraftRequest))]
[ProtoSerializable(typeof(SettleRequest))]
[ProtoSerializable(typeof(EventLayerRequest))]
[ProtoSerializable(typeof(SchedulePublishRequest))]
[ProtoSerializable(typeof(ShareRequest))]
[ProtoSerializable(typeof(PublishReply))]
[ProtoSerializable(typeof(DraftsDone))]
public partial class ConfigurationContractsModel : TypeModel;
