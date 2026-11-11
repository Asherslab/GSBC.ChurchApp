using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Prayer.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(Done))]
[ProtoSerializable(typeof(PrayerRequestSend))]
[ProtoSerializable(typeof(RequestRef))]
[ProtoSerializable(typeof(PrayMark))]
[ProtoSerializable(typeof(AnsweredSave))]
[ProtoSerializable(typeof(WallPost))]
[ProtoSerializable(typeof(YourRequest))]
[ProtoSerializable(typeof(SentOnPhone))]
[ProtoSerializable(typeof(PrayedOnPhone))]
[ProtoSerializable(typeof(KeptOnPhone))]
[ProtoSerializable(typeof(InboxRequest))]
[ProtoSerializable(typeof(Inbox))]
[ProtoSerializable(typeof(InboxLine))]
[ProtoSerializable(typeof(RequestDetails))]
[ProtoSerializable(typeof(CareNote))]
[ProtoSerializable(typeof(CareStep))]
[ProtoSerializable(typeof(CareChange))]
[ProtoSerializable(typeof(NoteSave))]
[ProtoSerializable(typeof(NoteRef))]
[ProtoSerializable(typeof(WallSave))]
[ProtoSerializable(typeof(WallCase))]
[ProtoSerializable(typeof(CareCase))]
[ProtoSerializable(typeof(QueuedOnPhone))]
public partial class PrayerContractsModel : TypeModel;
