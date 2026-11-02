using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Configuration.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(Placement))]
[ProtoSerializable(typeof(AreaQuery))]
[ProtoSerializable(typeof(AreaPlacements))]
[ProtoSerializable(typeof(SaveRequest))]
[ProtoSerializable(typeof(ChangeList))]
[ProtoSerializable(typeof(RollBackRequest))]
public partial class ConfigurationContractsModel : TypeModel;
