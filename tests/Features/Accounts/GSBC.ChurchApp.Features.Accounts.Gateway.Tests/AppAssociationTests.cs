using System.Text.Json;

namespace GSBC.ChurchApp.Features.Accounts.Gateway.Tests;

public sealed class AppAssociationTests
{
    private const string TeamId = "ABCDE12345";
    private const string UatBundleId = "au.com.baptist.app.uat";

    [Fact]
    public void TheAssociationFileNamesTheConfiguredTeamAndBundleAndOpensSharedLinks()
    {
        using JsonDocument association = JsonDocument.Parse(AppAssociation.Json(TeamId, UatBundleId));

        string app = $"{TeamId}.{UatBundleId}";
        Assert.Equal(app, association.RootElement.GetProperty("webcredentials").GetProperty("apps")[0].GetString());
        JsonElement details = association.RootElement.GetProperty("applinks").GetProperty("details")[0];
        Assert.Equal(app, details.GetProperty("appIDs")[0].GetString());
        Assert.Contains("/open/*", details.GetProperty("components").EnumerateArray().Select(component => component.GetProperty("/").GetString()));
    }
}
