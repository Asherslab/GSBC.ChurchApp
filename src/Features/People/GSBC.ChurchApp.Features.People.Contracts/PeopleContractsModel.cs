using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.People.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(MineRequest))]
[ProtoSerializable(typeof(MyPeople))]
[ProtoSerializable(typeof(PersonFamily))]
[ProtoSerializable(typeof(PersonCard))]
[ProtoSerializable(typeof(LinkAsk))]
[ProtoSerializable(typeof(SafeguardingRecord))]
[ProtoSerializable(typeof(BirthDateRequest))]
[ProtoSerializable(typeof(AddChildRequest))]
[ProtoSerializable(typeof(AskRequest))]
[ProtoSerializable(typeof(AnswerRequest))]
[ProtoSerializable(typeof(AskIdRequest))]
[ProtoSerializable(typeof(LinkRequest))]
[ProtoSerializable(typeof(PersonRequest))]
[ProtoSerializable(typeof(UpdatePersonRequest))]
[ProtoSerializable(typeof(SignInCodeReply))]
[ProtoSerializable(typeof(SafeguardingRequest))]
[ProtoSerializable(typeof(FlaggedRequest))]
[ProtoSerializable(typeof(FlaggedList))]
[ProtoSerializable(typeof(FlaggedPerson))]
[ProtoSerializable(typeof(Done))]
public partial class PeopleContractsModel : TypeModel;
