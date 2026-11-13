using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.News.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(Done))]
[ProtoSerializable(typeof(NewsPost))]
[ProtoSerializable(typeof(NewsSave))]
[ProtoSerializable(typeof(NewsRef))]
[ProtoSerializable(typeof(PublishRequest))]
[ProtoSerializable(typeof(AskAnswer))]
[ProtoSerializable(typeof(ReachRequest))]
[ProtoSerializable(typeof(NewsReach))]
[ProtoSerializable(typeof(DeskRequest))]
[ProtoSerializable(typeof(NewsList))]
public partial class NewsContractsModel : TypeModel;
