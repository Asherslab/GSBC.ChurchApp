using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Sermons.Import.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(ImportRequest))]
[ProtoSerializable(typeof(ImportRetry))]
[ProtoSerializable(typeof(ImportProgress))]
[ProtoSerializable(typeof(ImportItem))]
public partial class SermonsImportContractsModel : TypeModel;
