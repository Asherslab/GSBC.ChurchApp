using System.Text.Json.Serialization;

namespace GSBC.ChurchApp.Features.Outreach.Client.Phone;

public sealed class PhoneClaim
{
    public string Id { get; set; } = "";
    public string RouteId { get; set; } = "";
    public string DayId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public DateTime At { get; set; }
}

[JsonSerializable(typeof(List<PhoneClaim>))]
internal sealed partial class OutreachPhoneJson : JsonSerializerContext;
