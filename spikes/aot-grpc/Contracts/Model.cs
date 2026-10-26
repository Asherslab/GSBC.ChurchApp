using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Contracts;

/// <summary>
/// The compile-time serializer model. Message types reachable from the services are added by each
/// project's [ProtoGrpc]; list here only types serialized directly, outside gRPC.
/// </summary>
/// <remarks>
/// No client factory here: proxies and interceptors are generated per calling project (the generator
/// walks only its own assembly), so each project that calls or hosts a service declares its own.
/// </remarks>
// IL2095 is protobuf-net's own: TypeModel.Deserialize<T> and IProtoInput<T>.Deserialize<T> carry different
// [DynamicallyAccessedMembers] on T, and the trimmer reports the mismatch on whichever class derives
// from TypeModel - this one. The generated model never takes the reflective path those annotations guard.
[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path.")]
[ProtoModel]
[ProtoSerializable(typeof(EventsRequest))]
[ProtoSerializable(typeof(EventsReply))]
[ProtoSerializable(typeof(ChurchEvent))]
public partial class ContractsModel : TypeModel { }
