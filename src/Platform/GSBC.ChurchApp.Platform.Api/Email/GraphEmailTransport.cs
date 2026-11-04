using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GSBC.ChurchApp.Platform.Api.Email;

public sealed class GraphEmailOptions
{
    public const string Section = "Email:Graph";

    public required string TenantId { get; set; }
    public required string ClientId { get; set; }
    public required string Sender { get; set; }
    public required string Certificate { get; set; }
}

public sealed class GraphEmailTransport(
    HttpClient http,
    [FromKeyedServices(GraphEmailOptions.Section)] TokenCredential credential,
    IOptions<GraphEmailOptions> options) : IEmailTransport
{
    private static readonly TokenRequestContext GraphScope = new(["https://graph.microsoft.com/.default"]);

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        AccessToken token = await credential.GetTokenAsync(GraphScope, cancellationToken);
        string sendMail = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(options.Value.Sender)}/sendMail";
        using HttpRequestMessage request = new(HttpMethod.Post, sendMail);
        request.Content = JsonContent.Create(SendMailBody(message));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        using HttpResponseMessage response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string reason = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Graph refused the email: {(int)response.StatusCode} {reason}", null, response.StatusCode);
        }
    }

    private static JsonObject SendMailBody(EmailMessage message) => new()
    {
        ["message"] = new JsonObject
        {
            ["subject"] = message.Subject,
            ["body"] = new JsonObject { ["contentType"] = "Text", ["content"] = message.Body },
            ["toRecipients"] = new JsonArray(new JsonObject { ["emailAddress"] = new JsonObject { ["address"] = message.To } }),
        },
        ["saveToSentItems"] = true,
    };
}

public static class GraphEmailExtensions
{
    public static bool HasGraphEmail(this IConfiguration configuration) =>
        configuration.GetSection(GraphEmailOptions.Section).Exists();

    public static IServiceCollection AddGraphEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GraphEmailOptions>(configuration.GetSection(GraphEmailOptions.Section));
        services.AddKeyedSingleton<TokenCredential>(GraphEmailOptions.Section, (provider, _) =>
        {
            GraphEmailOptions graph = provider.GetRequiredService<IOptions<GraphEmailOptions>>().Value;
            return new ClientCertificateCredential(graph.TenantId, graph.ClientId, X509Certificate2.CreateFromPem(graph.Certificate, graph.Certificate));
        });
        services.AddHttpClient<IEmailTransport, GraphEmailTransport>();
        return services;
    }
}
