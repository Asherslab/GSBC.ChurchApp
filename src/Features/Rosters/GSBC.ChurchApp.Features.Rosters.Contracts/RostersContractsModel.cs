using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Rosters.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(Done))]
[ProtoSerializable(typeof(Nothing))]
[ProtoSerializable(typeof(RosterView))]
[ProtoSerializable(typeof(RosterSettings))]
[ProtoSerializable(typeof(Position))]
[ProtoSerializable(typeof(RosterDate))]
[ProtoSerializable(typeof(Assignment))]
[ProtoSerializable(typeof(SwapLine))]
[ProtoSerializable(typeof(TeamMember))]
[ProtoSerializable(typeof(PersonLine))]
[ProtoSerializable(typeof(AwaySpan))]
[ProtoSerializable(typeof(AwayList))]
[ProtoSerializable(typeof(AwayOf))]
[ProtoSerializable(typeof(RosterQuery))]
[ProtoSerializable(typeof(EventChoices))]
[ProtoSerializable(typeof(EventChoice))]
[ProtoSerializable(typeof(PositionSave))]
[ProtoSerializable(typeof(ItemRef))]
[ProtoSerializable(typeof(TrainedChange))]
[ProtoSerializable(typeof(SettingsSave))]
[ProtoSerializable(typeof(DateAdd))]
[ProtoSerializable(typeof(AssignRequest))]
[ProtoSerializable(typeof(ReplyRequest))]
[ProtoSerializable(typeof(SwapAsk))]
[ProtoSerializable(typeof(SwapTake))]
[ProtoSerializable(typeof(SwapRef))]
[ProtoSerializable(typeof(SwapDecision))]
[ProtoSerializable(typeof(AwaySave))]
[ProtoSerializable(typeof(AwayRef))]
public partial class RostersContractsModel : TypeModel;
