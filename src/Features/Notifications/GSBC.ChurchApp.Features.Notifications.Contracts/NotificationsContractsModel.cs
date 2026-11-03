using System.Diagnostics.CodeAnalysis;
using ProtoBuf;
using ProtoBuf.Meta;

namespace GSBC.ChurchApp.Features.Notifications.Contracts;

[UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "Annotation mismatch inside protobuf-net's TypeModel; the generated model does not use the reflective path (SPEC §9).")]
[ProtoModel]
[ProtoSerializable(typeof(Done))]
[ProtoSerializable(typeof(InboxItem))]
[ProtoSerializable(typeof(KindInfo))]
[ProtoSerializable(typeof(PreferenceItem))]
[ProtoSerializable(typeof(MarkReadRequest))]
[ProtoSerializable(typeof(RegisterDeviceRequest))]
[ProtoSerializable(typeof(AudienceMessage))]
[ProtoSerializable(typeof(Announcement))]
[ProtoSerializable(typeof(AnnouncementQuery))]
[ProtoSerializable(typeof(AnnouncementList))]
[ProtoSerializable(typeof(AnnouncementRequest))]
[ProtoSerializable(typeof(Reach))]
public partial class NotificationsContractsModel : TypeModel;
