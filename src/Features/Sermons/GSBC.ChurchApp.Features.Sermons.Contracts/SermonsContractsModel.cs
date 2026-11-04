using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(Done))]
[ProtoSerializable(typeof(Sermon))]
[ProtoSerializable(typeof(SeriesInfo))]
[ProtoSerializable(typeof(SpeakerInfo))]
[ProtoSerializable(typeof(ListeningEntry))]
[ProtoSerializable(typeof(ProgressBatch))]
[ProtoSerializable(typeof(SermonQuery))]
[ProtoSerializable(typeof(SermonList))]
[ProtoSerializable(typeof(SermonDetail))]
[ProtoSerializable(typeof(SermonCorrection))]
[ProtoSerializable(typeof(SeriesDetail))]
[ProtoSerializable(typeof(SeriesList))]
[ProtoSerializable(typeof(SpeakerDetail))]
[ProtoSerializable(typeof(SpeakerList))]
[ProtoSerializable(typeof(SourceStatus))]
[ProtoSerializable(typeof(SourceChoice))]
public partial class SermonsContractsModel : TypeModel;
