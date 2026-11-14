using GSBC.ChurchApp.Platform.Client.Uploads;

namespace GSBC.ChurchApp.Features.Events.Client;

public static class EventPictureSlots
{
    private const int EventSaveFields = 3;
    private const int RegularEventSaveFields = 3;
    private const int DateSaveFields = 4;
    private const int FieldsPicture = 3;
    private const int EventSaveUploadedField = 11;
    private const int RegularEventSaveUploadedField = 17;
    private const int DateSaveUploadedField = 10;

    public static readonly MediaSlot EventSave = MediaSlot.Ref(EventSaveFields, FieldsPicture);
    public static readonly MediaSlot RegularEventSave = MediaSlot.Ref(RegularEventSaveFields, FieldsPicture);
    public static readonly MediaSlot DateSave = MediaSlot.Ref(DateSaveFields, FieldsPicture);
    public static readonly MediaSlot EventSaveUploaded = MediaSlot.Id(EventSaveUploadedField);
    public static readonly MediaSlot RegularEventSaveUploaded = MediaSlot.Id(RegularEventSaveUploadedField);
    public static readonly MediaSlot DateSaveUploaded = MediaSlot.Id(DateSaveUploadedField);
}
