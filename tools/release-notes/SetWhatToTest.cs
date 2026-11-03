using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

if (args.Length != 4)
{
    Console.Error.WriteLine("usage: dotnet run SetWhatToTest.cs -- <bundle id> <version> <build number> <text file>");
    return 2;
}

(string bundleId, string version, string buildNumber, string textFile) = (args[0], args[1], args[2], args[3]);
string whatToTest = File.ReadAllText(textFile);
TimeSpan waitAtMost = TimeSpan.FromMinutes(60);
TimeSpan between = TimeSpan.FromSeconds(30);
const string Locale = "en-AU";

using HttpClient api = new() { BaseAddress = new Uri("https://api.appstoreconnect.apple.com/v1/") };

string appId = (await GetAsync($"apps?filter[bundleId]={bundleId}&limit=1"))["data"]!.AsArray().FirstOrDefault()?["id"]?.GetValue<string>()
    ?? throw new InvalidOperationException($"No app in App Store Connect has the bundle id {bundleId}.");

DateTime giveUpAt = DateTime.UtcNow + waitAtMost;
string? buildId = null;
while (true)
{
    JsonNode? build = (await GetAsync($"builds?filter[app]={appId}&filter[version]={buildNumber}&filter[preReleaseVersion.version]={version}&limit=1"))["data"]!.AsArray().FirstOrDefault();
    string? state = build?["attributes"]?["processingState"]?.GetValue<string>();
    Console.WriteLine($"Build {version} ({buildNumber}): {state ?? "not visible yet"}");
    if (state == "VALID")
    {
        buildId = build!["id"]!.GetValue<string>();
        break;
    }
    if (state is "FAILED" or "INVALID")
        throw new InvalidOperationException($"Apple marked build {buildNumber} {state}; there is nothing to describe.");
    if (DateTime.UtcNow > giveUpAt)
        throw new TimeoutException($"Build {buildNumber} was still processing after {waitAtMost.TotalMinutes} minutes; set What to Test by hand in App Store Connect.");
    await Task.Delay(between);
}

JsonArray localizations = (await GetAsync($"builds/{buildId}/betaBuildLocalizations"))["data"]!.AsArray();
if (localizations.Count == 0)
{
    await SendAsync(HttpMethod.Post, "betaBuildLocalizations", new JsonObject
    {
        ["data"] = new JsonObject
        {
            ["type"] = "betaBuildLocalizations",
            ["attributes"] = new JsonObject { ["locale"] = Locale, ["whatsNew"] = whatToTest },
            ["relationships"] = new JsonObject { ["build"] = new JsonObject { ["data"] = new JsonObject { ["type"] = "builds", ["id"] = buildId } } },
        },
    });
}
foreach (JsonNode? localization in localizations)
{
    string id = localization!["id"]!.GetValue<string>();
    await SendAsync(HttpMethod.Patch, $"betaBuildLocalizations/{id}", new JsonObject
    {
        ["data"] = new JsonObject { ["type"] = "betaBuildLocalizations", ["id"] = id, ["attributes"] = new JsonObject { ["whatsNew"] = whatToTest } },
    });
}
Console.WriteLine($"What to Test is set on build {version} ({buildNumber}).");
return 0;

async Task<JsonNode> GetAsync(string path)
{
    using HttpRequestMessage request = new(HttpMethod.Get, path);
    return await ReadAsync(request);
}

async Task SendAsync(HttpMethod method, string path, JsonObject body)
{
    using HttpRequestMessage request = new(method, path) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
    await ReadAsync(request);
}

async Task<JsonNode> ReadAsync(HttpRequestMessage request)
{
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token());
    using HttpResponseMessage response = await api.SendAsync(request);
    string text = await response.Content.ReadAsStringAsync();
    if (!response.IsSuccessStatusCode)
        throw new HttpRequestException($"{request.Method} {request.RequestUri}: {(int)response.StatusCode} {text}");
    return JsonNode.Parse(text.Length == 0 ? "{}" : text)!;
}

static string Token()
{
    string keyId = Environment.GetEnvironmentVariable("ASC_KEY_ID") ?? throw new InvalidOperationException("Set ASC_KEY_ID.");
    string issuer = Environment.GetEnvironmentVariable("ASC_ISSUER_ID") ?? throw new InvalidOperationException("Set ASC_ISSUER_ID.");
    string keyPath = Environment.GetEnvironmentVariable("ASC_KEY_PATH") ?? throw new InvalidOperationException("Set ASC_KEY_PATH.");
    long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    string header = Base64Url(new JsonObject { ["alg"] = "ES256", ["kid"] = keyId, ["typ"] = "JWT" }.ToJsonString());
    string claims = Base64Url(new JsonObject { ["iss"] = issuer, ["iat"] = now, ["exp"] = now + 1200, ["aud"] = "appstoreconnect-v1" }.ToJsonString());
    using ECDsa key = ECDsa.Create();
    key.ImportFromPem(File.ReadAllText(keyPath));
    byte[] signature = key.SignData(Encoding.ASCII.GetBytes($"{header}.{claims}"), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
    return $"{header}.{claims}.{Base64Url(signature)}";
}

static string Base64Url(object value) =>
    Convert.ToBase64String(value is string text ? Encoding.UTF8.GetBytes(text) : (byte[])value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
