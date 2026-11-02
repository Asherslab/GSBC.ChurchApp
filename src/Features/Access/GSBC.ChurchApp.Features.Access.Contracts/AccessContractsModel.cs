using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Access.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(Nothing))]
[ProtoSerializable(typeof(Done))]
[ProtoSerializable(typeof(GrantList))]
[ProtoSerializable(typeof(Catalog))]
[ProtoSerializable(typeof(RoleList))]
[ProtoSerializable(typeof(RoleInfo))]
[ProtoSerializable(typeof(RoleReference))]
[ProtoSerializable(typeof(AssignmentQuery))]
[ProtoSerializable(typeof(AssignmentList))]
[ProtoSerializable(typeof(AssignmentInfo))]
[ProtoSerializable(typeof(AssignRequest))]
[ProtoSerializable(typeof(AssignmentReference))]
[ProtoSerializable(typeof(PersonReference))]
[ProtoSerializable(typeof(PersonAbilities))]
[ProtoSerializable(typeof(RolePreviewRequest))]
[ProtoSerializable(typeof(RoleEffect))]
public partial class AccessContractsModel : TypeModel;
