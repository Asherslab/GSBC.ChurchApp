using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Analytics.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(ReportContext))]
[ProtoSerializable(typeof(Received))]
[ProtoSerializable(typeof(DiagnosticsReport))]
[ProtoSerializable(typeof(ErrorOccurrence))]
[ProtoSerializable(typeof(FailedCallCount))]
[ProtoSerializable(typeof(HealthQuery))]
[ProtoSerializable(typeof(HealthOverview))]
[ProtoSerializable(typeof(ErrorKindSummary))]
[ProtoSerializable(typeof(FailedCallSummary))]
[ProtoSerializable(typeof(ErrorKindRequest))]
[ProtoSerializable(typeof(ErrorKindDetail))]
[ProtoSerializable(typeof(CountByLabel))]
[ProtoSerializable(typeof(CountByDay))]
[ProtoSerializable(typeof(UsageReport))]
[ProtoSerializable(typeof(UsageCount))]
[ProtoSerializable(typeof(Tally))]
[ProtoSerializable(typeof(InsightsQuery))]
[ProtoSerializable(typeof(InsightsSummary))]
[ProtoSerializable(typeof(GroupTally))]
[ProtoSerializable(typeof(WeekPoint))]
[ProtoSerializable(typeof(TopList))]
[ProtoSerializable(typeof(LabelTally))]
public partial class AnalyticsContractsModel : TypeModel;
