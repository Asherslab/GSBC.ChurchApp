using System.Text.RegularExpressions;

namespace GSBC.ChurchApp.Features.People.Api.SafeMinistry;

public sealed partial class SafeMinistryOptions
{
    public const string Section = "People:SafeMinistryCheck";

    public string Subdomain { get; set; } = "";
    public string ApiKey { get; set; } = "";

    public bool Connected => ApiKey.Trim().Length > 0 && SubdomainShape().IsMatch(Subdomain.Trim());

    public Uri BaseAddress => new($"https://{Subdomain.Trim().ToLowerInvariant()}.safeministrycheck.com.au/api/");

    [GeneratedRegex("^[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?$")]
    private static partial Regex SubdomainShape();
}
