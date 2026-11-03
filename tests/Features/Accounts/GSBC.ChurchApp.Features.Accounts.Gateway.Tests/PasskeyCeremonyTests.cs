using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using GSBC.ChurchApp.Features.Accounts.Data;
using GSBC.ChurchApp.Platform.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace GSBC.ChurchApp.Features.Accounts.Gateway.Tests;

public sealed class PasskeyCeremonyTests : IAsyncLifetime
{
    private const string Origin = "https://localhost";
    private const string TeamId = "ABCDE12345";
    private const string UatBundleId = "au.com.baptist.app.uat";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private readonly ECDsa _tokenKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private WebApplication _app = null!;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{Database.ConnectionStringName}"] = _postgres.GetConnectionString(),
            ["Accounts:Passkeys:RelyingPartyId"] = "localhost",
            ["Accounts:Passkeys:AllowedOrigins:0"] = Origin,
            [AccountTokens.SigningKeySetting] = _tokenKey.ExportPkcs8PrivateKeyPem(),
            [$"{AppAssociation.Section}:TeamId"] = TeamId,
            [$"{AppAssociation.Section}:BundleId"] = UatBundleId,
        });
        builder.Services.AddAccountsGateway(builder.Configuration).AddSyncData();

        _app = builder.Build();
        _app.UseAppAssociation();
        _app.UseRouting();
        _app.UseAuthentication();
        _app.UseRateLimiter();
        _app.MapPasskeyCeremonies();
        _app.MapGet("/test/token", (HttpContext http, ApiTokenIssuer tokens) =>
            http.User.Identity?.IsAuthenticated == true ? Results.Text(tokens.Issue(http.User)) : Results.Unauthorized());
        _app.MapFallback(() => Results.Text("<!DOCTYPE html>", "text/html"));
        await _app.StartAsync();

        await using AsyncServiceScope scope = _app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SyncDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().Database.MigrateAsync();
    }

    [Fact]
    public async Task SignUpSignInAndEndASession()
    {
        (Guid userId, string ticket) = await NewAccountWithTicketAsync("ada@example.com");
        using SoftwareAuthenticator authenticator = new(Origin);

        using HttpClient firstBrowser = Browser();
        await RegisterAsync(firstBrowser, authenticator, ticket);
        JsonWebToken firstToken = await ValidTokenAsync(firstBrowser);
        Assert.Equal(userId.ToString(), firstToken.Subject);

        using HttpClient secondBrowser = Browser();
        string assertion = authenticator.Assert(await PostForJsonAsync(secondBrowser, "request-options", new { }));
        Assert.Equal(HttpStatusCode.OK, (await Post(secondBrowser, "sign-in", new { credential = assertion })).StatusCode);
        JsonWebToken secondToken = await ValidTokenAsync(secondBrowser);
        Assert.NotEqual(firstToken.GetClaim(AccountClaims.SessionId).Value, secondToken.GetClaim(AccountClaims.SessionId).Value);
        Assert.Equal(2, await SessionCountAsync(userId));

        await EndSessionAsync(Guid.Parse(secondToken.GetClaim(AccountClaims.SessionId).Value));
        Assert.Equal(HttpStatusCode.Unauthorized, (await secondBrowser.GetAsync("/test/token", TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await firstBrowser.GetAsync("/test/token", TestContext.Current.CancellationToken)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(Browser(), "sign-in", new { credential = assertion })).StatusCode);
    }

    [Fact]
    public async Task RefusesAForeignOriginAUsedTicketAndADisabledAccount()
    {
        (Guid userId, string ticket) = await NewAccountWithTicketAsync("grace@example.com");
        using SoftwareAuthenticator authenticator = new(Origin);

        using HttpClient noOrigin = Browser(origin: null);
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(noOrigin, "request-options", new { })).StatusCode);

        using HttpClient browser = Browser();
        await RegisterAsync(browser, authenticator, ticket);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Post(Browser(), "creation-options", new { ticket })).StatusCode);

        await DisableAsync(userId);
        using HttpClient another = Browser();
        string assertion = authenticator.Assert(await PostForJsonAsync(another, "request-options", new { }));
        Assert.Equal(HttpStatusCode.Forbidden, (await Post(another, "sign-in", new { credential = assertion })).StatusCode);
    }

    [Fact]
    public async Task ThePhoneKeepsASessionSecretInsteadOfACookieUntilTheSessionEnds()
    {
        (Guid userId, string ticket) = await NewAccountWithTicketAsync("phoebe@example.com");
        using SoftwareAuthenticator authenticator = new(Origin);
        using HttpClient phone = Browser();

        string credential = authenticator.Create(await PostForJsonAsync(phone, "creation-options", new { ticket }));
        PasskeyEndpoints.CeremonyReply? signedUp = await (await Post(phone, "register", new { credential, ticket, name = "iPhone", app = true }))
            .Content.ReadFromJsonAsync<PasskeyEndpoints.CeremonyReply>(TestContext.Current.CancellationToken);
        string secret = Assert.IsType<string>(signedUp?.Session);

        Assert.Equal(HttpStatusCode.Unauthorized, (await phone.GetAsync("/test/token", TestContext.Current.CancellationToken)).StatusCode);
        HttpResponseMessage withSecret = await TokenWithSessionAsync(phone, secret);
        Assert.Equal(HttpStatusCode.OK, withSecret.StatusCode);
        JsonWebToken token = new(await withSecret.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(userId.ToString(), token.Subject);

        await EndSessionAsync(Guid.Parse(token.GetClaim(AccountClaims.SessionId).Value));
        Assert.Equal(HttpStatusCode.Unauthorized, (await TokenWithSessionAsync(phone, secret)).StatusCode);

        string assertion = authenticator.Assert(await PostForJsonAsync(phone, "request-options", new { }));
        PasskeyEndpoints.CeremonyReply? signedIn = await (await Post(phone, "sign-in", new { credential = assertion, app = true, device = "iPhone" }))
            .Content.ReadFromJsonAsync<PasskeyEndpoints.CeremonyReply>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, (await TokenWithSessionAsync(phone, Assert.IsType<string>(signedIn?.Session))).StatusCode);
    }

    [Fact]
    public async Task TheAssociationFileNamesTheConfiguredTeamAndBundle()
    {
        HttpResponseMessage response = await Browser().GetAsync(AppAssociation.RequestPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument association = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        string app = $"{TeamId}.{UatBundleId}";
        Assert.Equal(app, association.RootElement.GetProperty("webcredentials").GetProperty("apps")[0].GetString());
        Assert.Equal(app, association.RootElement.GetProperty("applinks").GetProperty("details")[0].GetProperty("appIDs")[0].GetString());
    }

    private static Task<HttpResponseMessage> TokenWithSessionAsync(HttpClient phone, string secret)
    {
        HttpRequestMessage request = new(HttpMethod.Get, "/test/token");
        request.Headers.Add(AppSessions.Header, secret);
        return phone.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private async Task RegisterAsync(HttpClient browser, SoftwareAuthenticator authenticator, string ticket)
    {
        string credential = authenticator.Create(await PostForJsonAsync(browser, "creation-options", new { ticket }));
        HttpResponseMessage registered = await Post(browser, "register", new { credential, ticket, name = "Test laptop" });
        Assert.True(registered.IsSuccessStatusCode, await registered.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private async Task<JsonWebToken> ValidTokenAsync(HttpClient browser)
    {
        string token = await browser.GetStringAsync("/test/token", TestContext.Current.CancellationToken);
        TokenValidationResult result = await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = AccountTokens.Issuer,
            ValidAudience = AccountTokens.Audience,
            IssuerSigningKey = new ECDsaSecurityKey(_tokenKey),
        });
        Assert.True(result.IsValid, result.Exception?.Message);
        return (JsonWebToken)result.SecurityToken;
    }

    private async Task<(Guid UserId, string Ticket)> NewAccountWithTicketAsync(string email)
    {
        await using AsyncServiceScope scope = _app.Services.CreateAsyncScope();
        UserManager<AccountUser> users = scope.ServiceProvider.GetRequiredService<UserManager<AccountUser>>();
        Guid id = Guid.CreateVersion7();
        Assert.True((await users.CreateAsync(new AccountUser { Id = id, UserName = id.ToString(), Email = email, DisplayName = "Test" })).Succeeded);

        string ticket = scope.ServiceProvider.GetRequiredService<OneTimeCodes>().IssueToken(id, CodePurpose.RegisterPasskey, OneTimeCodes.TicketLifetime);
        await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().SaveChangesAsync();
        return (id, ticket);
    }

    private async Task<int> SessionCountAsync(Guid userId)
    {
        await using AsyncServiceScope scope = _app.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().Sessions.CountAsync(session => session.UserId == userId);
    }

    private async Task EndSessionAsync(Guid sessionId)
    {
        await using AsyncServiceScope scope = _app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().Sessions.Where(session => session.Id == sessionId).ExecuteDeleteAsync();
    }

    private async Task DisableAsync(Guid userId)
    {
        await using AsyncServiceScope scope = _app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AccountsDbContext>().Users.Where(user => user.Id == userId)
            .ExecuteUpdateAsync(update => update.SetProperty(user => user.Disabled, true));
    }

    private HttpClient Browser(string? origin = Origin)
    {
        HttpClient client = new(new CookieContainerHandler { InnerHandler = _app.GetTestServer().CreateHandler() })
        {
            BaseAddress = new Uri("https://localhost"),
        };
        if (origin is not null)
            client.DefaultRequestHeaders.Add("Origin", origin);
        return client;
    }

    private static Task<HttpResponseMessage> Post(HttpClient browser, string ceremony, object body) =>
        browser.PostAsJsonAsync($"{PasskeyEndpoints.Prefix}/{ceremony}", body, TestContext.Current.CancellationToken);

    private static async Task<string> PostForJsonAsync(HttpClient browser, string ceremony, object body)
    {
        HttpResponseMessage response = await Post(browser, ceremony, body);
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.IsSuccessStatusCode, content);
        return content;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        await _postgres.DisposeAsync();
        _tokenKey.Dispose();
    }
}
