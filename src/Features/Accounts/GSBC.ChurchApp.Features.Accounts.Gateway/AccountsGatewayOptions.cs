namespace GSBC.ChurchApp.Features.Accounts.Gateway;

public sealed class AccountsGatewayOptions
{
    public const string Section = "Accounts:Passkeys";

    public string RelyingPartyId { get; set; } = "";
    public string[] AllowedOrigins { get; set; } = [];
}
