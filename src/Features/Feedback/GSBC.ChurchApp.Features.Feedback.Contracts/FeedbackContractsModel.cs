using System.Diagnostics.CodeAnalysis;
using GSBC.ChurchApp.Platform.Contracts.Media;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Feedback.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(Done))]
[ProtoSerializable(typeof(PhoneDetails))]
[ProtoSerializable(typeof(FeedbackSend))]
[ProtoSerializable(typeof(DeskQuery))]
[ProtoSerializable(typeof(DeskList))]
[ProtoSerializable(typeof(StatusCount))]
[ProtoSerializable(typeof(TicketLine))]
[ProtoSerializable(typeof(TicketRef))]
[ProtoSerializable(typeof(Ticket))]
[ProtoSerializable(typeof(TicketNote))]
[ProtoSerializable(typeof(TicketStep))]
[ProtoSerializable(typeof(StatusChange))]
[ProtoSerializable(typeof(NoteSave))]
[ProtoSerializable(typeof(NoteRef))]
[ProtoSerializable(typeof(BlockSave))]
[ProtoSerializable(typeof(PhoneRef))]
[ProtoSerializable(typeof(BlockedQuery))]
[ProtoSerializable(typeof(BlockedPhones))]
[ProtoSerializable(typeof(BlockedPhone))]
[ProtoSerializable(typeof(SentOnPhone))]
[ProtoSerializable(typeof(SentFeedback))]
[ProtoSerializable(typeof(MediaRef))]
public partial class FeedbackContractsModel : TypeModel;
