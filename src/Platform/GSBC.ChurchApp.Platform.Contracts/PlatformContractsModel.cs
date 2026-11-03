using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Contracts.Destinations;
using GSBC.ChurchApp.Platform.Contracts.Sync;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Platform.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(PullRequest))]
[ProtoSerializable(typeof(PullReply))]
[ProtoSerializable(typeof(WatchRequest))]
[ProtoSerializable(typeof(WatchEvent))]
[ProtoSerializable(typeof(Destination))]
[ProtoSerializable(typeof(DestinationSearch))]
[ProtoSerializable(typeof(DestinationGroup))]
[ProtoSerializable(typeof(DestinationResults))]
[ProtoSerializable(typeof(DestinationRequest))]
public partial class PlatformContractsModel : TypeModel;
