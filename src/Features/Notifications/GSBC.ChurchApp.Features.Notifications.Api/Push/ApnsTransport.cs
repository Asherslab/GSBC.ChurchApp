using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Features.Notifications.Api.Push;

internal sealed class ApnsTransport(HttpClient http, IOptions<ApnsOptions> options, TimeProvider time) : IPushTransport, IDisposable
{
    public const string SandboxEnvironment = "sandbox";
    private const string SandboxHost = "https://api.sandbox.push.apple.com";
    private const string ProductionHost = "https://api.push.apple.com";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(50);
    private static readonly HashSet<string> DeadReasons = ["BadDeviceToken", "Unregistered", "DeviceTokenNotForTopic"];

    private readonly Lock _gate = new();
    private readonly ECDsa _key = LoadKey(options.Value.Key!);
    private (string Token, DateTimeOffset IssuedAt)? _bearer;

    public async Task<PushResult> SendAsync(PushTarget target, PushMessage message, CancellationToken cancellationToken)
    {
        string host = target.Environment == SandboxEnvironment ? SandboxHost : ProductionHost;
        using HttpRequestMessage request = new(HttpMethod.Post, $"{host}/3/device/{target.Token}")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new StringContent(Payload(message), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", Bearer());
        request.Headers.Add("apns-topic", target.AppId);
        request.Headers.Add("apns-push-type", message.Background ? "background" : "alert");
        request.Headers.Add("apns-priority", message.Background ? "5" : "10");
        if (message.CollapseId is { Length: > 0 and <= 64 } collapse)
            request.Headers.Add("apns-collapse-id", collapse);

        try
        {
            using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return PushResult.Sent;
            string reason = await ReasonAsync(response, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.Gone => new PushResult(PushOutcome.DeadToken, reason),
                HttpStatusCode.BadRequest when DeadReasons.Contains(reason) => new PushResult(PushOutcome.DeadToken, reason),
                HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError => new PushResult(PushOutcome.Retry, reason),
                HttpStatusCode.Forbidden when reason is "ExpiredProviderToken" => ForgetBearer(reason),
                _ => new PushResult(PushOutcome.Failed, $"{(int)response.StatusCode} {reason}"),
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return new PushResult(PushOutcome.Retry, exception.Message);
        }
    }

    public static string Payload(PushMessage message)
    {
        JsonObject aps = message.Background
            ? new JsonObject { ["content-available"] = 1 }
            : new JsonObject
            {
                ["alert"] = new JsonObject { ["title"] = message.Title, ["body"] = message.Body },
                ["sound"] = "default",
            };
        if (!message.Background && message.Badge is { } badge)
            aps["badge"] = badge;
        if (message.ThreadId is { } thread)
            aps["thread-id"] = thread;
        if (message.Urgent)
            aps["interruption-level"] = "time-sensitive";

        JsonObject payload = new() { ["aps"] = aps };
        if (message.Link is { } link)
            payload["link"] = link;
        if (message.NotificationId is { } id)
            payload["notification"] = id;
        return payload.ToJsonString();
    }

    private string Bearer()
    {
        lock (_gate)
        {
            DateTimeOffset now = time.GetUtcNow();
            if (_bearer is { } current && now - current.IssuedAt < TokenLifetime)
                return current.Token;

            string header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string> { ["alg"] = "ES256", ["kid"] = options.Value.KeyId! }));
            string claims = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
            {
                ["iss"] = options.Value.TeamId!,
                ["iat"] = now.ToUnixTimeSeconds(),
            }));
            byte[] signature = _key.SignData(Encoding.ASCII.GetBytes($"{header}.{claims}"), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            string token = $"{header}.{claims}.{Base64Url(signature)}";
            _bearer = (token, now);
            return token;
        }
    }

    private PushResult ForgetBearer(string reason)
    {
        lock (_gate)
            _bearer = null;
        return new PushResult(PushOutcome.Retry, reason);
    }

    private static async Task<string> ReasonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using JsonDocument body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            return body.RootElement.TryGetProperty("reason", out JsonElement reason) ? reason.GetString() ?? "" : "";
        }
        catch (JsonException)
        {
            return "";
        }
    }

    private static ECDsa LoadKey(string pem)
    {
        ECDsa key = ECDsa.Create();
        key.ImportFromPem(pem);
        return key;
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public void Dispose() => _key.Dispose();
}
