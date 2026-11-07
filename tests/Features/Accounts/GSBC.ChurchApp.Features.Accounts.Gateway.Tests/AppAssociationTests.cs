using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace GSBC.ChurchApp.Features.Accounts.Gateway.Tests;

public sealed class AppAssociationTests : IAsyncLifetime
{
    private const string TeamId = "ABCDE12345";
    private const string UatBundleId = "au.com.baptist.app.uat";

    private WebApplication _gateway = null!;

    public async ValueTask InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"{AppAssociation.Section}:TeamId"] = TeamId,
            [$"{AppAssociation.Section}:BundleId"] = UatBundleId,
        });
        _gateway = builder.Build();
        _gateway.UseAppAssociation();
        _gateway.UseRouting();
        _gateway.MapFallback(() => Results.Text("<!DOCTYPE html>", "text/html"));
        await _gateway.StartAsync();
    }

    [Fact]
    public async Task TheAssociationFileIsServedAsJsonNamingTheConfiguredTeamAndBundleAndOpensSharedLinks()
    {
        HttpResponseMessage response = await _gateway.GetTestClient().GetAsync(AppAssociation.RequestPath, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using JsonDocument association = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        string app = $"{TeamId}.{UatBundleId}";
        Assert.Equal(app, association.RootElement.GetProperty("webcredentials").GetProperty("apps")[0].GetString());
        JsonElement details = association.RootElement.GetProperty("applinks").GetProperty("details")[0];
        Assert.Equal(app, details.GetProperty("appIDs")[0].GetString());
        Assert.Contains("/open/*", details.GetProperty("components").EnumerateArray().Select(component => component.GetProperty("/").GetString()));
    }

    public async ValueTask DisposeAsync() => await _gateway.DisposeAsync();
}
