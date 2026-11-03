namespace GSBC.ChurchApp.Platform.Api.Email;

public static class ReservedAddresses
{
    public const string NotSent = "Not sent: a reserved test address (RFC 2606) never receives mail.";

    private static readonly string[] Domains = ["example.com", "example.net", "example.org", "test", "example", "invalid", "localhost"];

    public static bool IsReserved(string address)
    {
        int at = address.LastIndexOf('@');
        string domain = address[(at + 1)..].Trim().TrimEnd('.').ToLowerInvariant();
        return Domains.Any(reserved => domain == reserved || domain.EndsWith("." + reserved, StringComparison.Ordinal));
    }
}
