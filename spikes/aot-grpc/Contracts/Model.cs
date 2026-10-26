using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path.")]
[ProtoModel]
[ProtoSerializable(typeof(EventsRequest))]
[ProtoSerializable(typeof(EventsReply))]
[ProtoSerializable(typeof(ChurchEvent))]
public partial class ContractsModel : TypeModel { }
