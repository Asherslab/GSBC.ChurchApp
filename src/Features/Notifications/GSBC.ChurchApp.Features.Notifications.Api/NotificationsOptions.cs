namespace GSBC.ChurchApp.Features.Notifications.Api;

public sealed class ApnsOptions
{
    public const string Section = "Notifications:Apns";

    public string? KeyId { get; set; }
    public string? TeamId { get; set; }
    public string? Key { get; set; }

    public bool IsConfigured => KeyId is { Length: > 0 } && TeamId is { Length: > 0 } && Key is { Length: > 0 };
}
